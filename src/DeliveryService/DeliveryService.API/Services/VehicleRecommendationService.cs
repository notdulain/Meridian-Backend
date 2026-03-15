using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeliveryService.API.DTOs;
using DeliveryService.API.Repositories;
using DeliveryService.API.Models;
using Meridian.VehicleGrpc;
using Grpc.Core;

namespace DeliveryService.API.Services;

public class VehicleRecommendationService : IVehicleRecommendationService
{
    private readonly DeliveryRepository _deliveryRepository;
    private readonly VehicleGrpc.VehicleGrpcClient _vehicleClient;
    private readonly ILogger<VehicleRecommendationService> _logger;

    public VehicleRecommendationService(
        DeliveryRepository deliveryRepository,
        VehicleGrpc.VehicleGrpcClient vehicleClient,
        ILogger<VehicleRecommendationService> logger)
    {
        _deliveryRepository = deliveryRepository;
        _vehicleClient = vehicleClient;
        _logger = logger;
    }

    public async Task<IEnumerable<VehicleRecommendationDto>> GetRecommendedVehiclesAsync(int deliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await _deliveryRepository.GetByIdAsync(deliveryId, cancellationToken);
        if (delivery == null)
            return Array.Empty<VehicleRecommendationDto>();

        var requiredWeight = (double)delivery.PackageWeightKg;
        var requiredVolume = (double)delivery.PackageVolumeM3;

        var availableVehicles = await GetAvailableVehiclesFromServiceAsync(cancellationToken);

        var recommendedVehicles = new List<VehicleRecommendationDto>();

        foreach (var vehicle in availableVehicles)
        {
            var isCapacityOk = vehicle.CapacityKg >= requiredWeight && vehicle.CapacityM3 >= requiredVolume;
            var isAvailable = string.Equals(vehicle.Status, "Available", StringComparison.OrdinalIgnoreCase);

            if (isCapacityOk && isAvailable)
            {
                var matchScore = CalculateMatchScore(vehicle, requiredWeight, requiredVolume);
                
                recommendedVehicles.Add(new VehicleRecommendationDto
                {
                    VehicleId = vehicle.VehicleId,
                    PlateNumber = vehicle.PlateNumber,
                    Make = vehicle.Make,
                    Model = vehicle.Model,
                    CapacityKg = vehicle.CapacityKg,
                    CapacityM3 = vehicle.CapacityM3,
                    FuelEfficiencyKmPerLitre = vehicle.FuelEfficiencyKmPerLitre,
                    MatchScore = matchScore,
                    RecommendationReason = $"Sufficient capacity. Ranked by score {matchScore:F2}."
                });
            }
        }

        return recommendedVehicles
            .OrderByDescending(v => v.MatchScore)
            .ToList();
    }

    private double CalculateMatchScore(VehicleResponse vehicle, double requiredWeight, double requiredVolume)
    {
        // Simple heuristic: higher fuel efficiency is better.
        // Also could penalize significantly oversized vehicles to save them for larger loads.
        var weightUtilization = requiredWeight / vehicle.CapacityKg;
        var volumeUtilization = requiredVolume / vehicle.CapacityM3;
        
        // A simple weighted score favoring efficiency and good utilization 
        // (0.0 to 1.0 mostly, where 1.0 is full utilization and great efficiency)
        var utilizationScore = (weightUtilization + volumeUtilization) / 2.0;
        
        var efficiencyScore = Math.Min(vehicle.FuelEfficiencyKmPerLitre / 20.0, 1.0); // Normalize to ~20 km/L max

        return (utilizationScore * 0.4) + (efficiencyScore * 0.6);
    }

    private async Task<List<VehicleResponse>> GetAvailableVehiclesFromServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _vehicleClient.GetAvailableVehiclesAsync(new GetAvailableVehiclesRequest(), cancellationToken: cancellationToken);
            return response.Vehicles.ToList();
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error getting available vehicles from VehicleService via gRPC. Status: {Status}", ex.Status);
            throw new InvalidOperationException("VehicleService is unavailable for recommendations.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting available vehicles from VehicleService");
            throw new InvalidOperationException("VehicleService is unavailable for recommendations.", ex);
        }
    }
}
