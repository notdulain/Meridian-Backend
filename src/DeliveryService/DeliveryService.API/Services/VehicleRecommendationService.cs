using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly IRouteDistanceService _routeDistanceService;
    private readonly ILogger<VehicleRecommendationService> _logger;

    public VehicleRecommendationService(
        DeliveryRepository deliveryRepository,
        VehicleGrpc.VehicleGrpcClient vehicleClient,
        IRouteDistanceService routeDistanceService,
        ILogger<VehicleRecommendationService> logger)
    {
        _deliveryRepository = deliveryRepository;
        _vehicleClient = vehicleClient;
        _routeDistanceService = routeDistanceService;
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

        var eligibleVehicles = availableVehicles
            .Where(vehicle =>
                vehicle.CapacityKg >= requiredWeight &&
                vehicle.CapacityM3 >= requiredVolume &&
                string.Equals(vehicle.Status, "Available", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var recommendationTasks = eligibleVehicles
            .Select(vehicle => BuildRecommendationAsync(vehicle, delivery.PickupAddress, requiredWeight, requiredVolume, cancellationToken));

        var recommendedVehicles = await Task.WhenAll(recommendationTasks);

        return recommendedVehicles
            .OrderByDescending(v => v.MatchScore)
            .ThenBy(v => v.DistanceToPickupKm ?? double.MaxValue)
            .ToList();
    }

    private async Task<VehicleRecommendationDto> BuildRecommendationAsync(
        VehicleResponse vehicle,
        string pickupAddress,
        double requiredWeight,
        double requiredVolume,
        CancellationToken cancellationToken)
    {
        double? distanceToPickupKm = null;

        if (!string.IsNullOrWhiteSpace(vehicle.CurrentLocation) && !string.IsNullOrWhiteSpace(pickupAddress))
        {
            try
            {
                distanceToPickupKm = await _routeDistanceService.GetDistanceInKilometersAsync(vehicle.CurrentLocation, pickupAddress, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unable to calculate route distance for vehicle {VehicleId} from {Origin} to {Destination}",
                    vehicle.VehicleId,
                    vehicle.CurrentLocation,
                    pickupAddress);
            }
        }

        var matchScore = CalculateMatchScore(vehicle, requiredWeight, requiredVolume, distanceToPickupKm);

        return new VehicleRecommendationDto
        {
            VehicleId = vehicle.VehicleId,
            PlateNumber = vehicle.PlateNumber,
            Make = vehicle.Make,
            Model = vehicle.Model,
            CapacityKg = vehicle.CapacityKg,
            CapacityM3 = vehicle.CapacityM3,
            FuelEfficiencyKmPerLitre = vehicle.FuelEfficiencyKmPerLitre,
            CurrentLocation = vehicle.CurrentLocation,
            DistanceToPickupKm = distanceToPickupKm,
            MatchScore = matchScore,
            RecommendationReason = BuildRecommendationReason(matchScore, distanceToPickupKm)
        };
    }

    private static double CalculateMatchScore(VehicleResponse vehicle, double requiredWeight, double requiredVolume, double? distanceToPickupKm)
    {
        var weightUtilization = requiredWeight / vehicle.CapacityKg;
        var volumeUtilization = requiredVolume / vehicle.CapacityM3;
        var utilizationScore = (weightUtilization + volumeUtilization) / 2.0;
        var efficiencyScore = Math.Min(vehicle.FuelEfficiencyKmPerLitre / 20.0, 1.0);
        var distanceScore = distanceToPickupKm.HasValue
            ? 1d / (1d + distanceToPickupKm.Value)
            : 0d;

        return Math.Round((utilizationScore * 0.35) + (efficiencyScore * 0.35) + (distanceScore * 0.30), 4);
    }

    private static string BuildRecommendationReason(double matchScore, double? distanceToPickupKm)
    {
        var distancePart = distanceToPickupKm.HasValue
            ? $"Distance to pickup: {distanceToPickupKm.Value:F2} km."
            : "Distance to pickup unavailable.";

        return $"{distancePart} Ranked by blended score {matchScore:F2}.";
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
