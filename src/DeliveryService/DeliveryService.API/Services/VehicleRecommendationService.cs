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

namespace DeliveryService.API.Services;

public class VehicleRecommendationService : IVehicleRecommendationService
{
    private readonly DeliveryRepository _deliveryRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VehicleRecommendationService> _logger;

    public VehicleRecommendationService(
        DeliveryRepository deliveryRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<VehicleRecommendationService> logger)
    {
        _deliveryRepository = deliveryRepository;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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

    private double CalculateMatchScore(VehicleDto vehicle, double requiredWeight, double requiredVolume)
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

    private async Task<List<VehicleDto>> GetAvailableVehiclesFromServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("VehicleServiceClient");
            var baseUrl = _configuration["ServiceUrls:VehicleService"] ?? 
                          _configuration["ServiceUrls:FleetService"] ?? 
                          "http://localhost:6002";
                         
            var response = await client.GetAsync($"{baseUrl}/api/vehicles/available", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch available vehicles. Status Code: {StatusCode}", response.StatusCode);
                return new List<VehicleDto>();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<VehicleDto>>>(cancellationToken: cancellationToken);
            return apiResponse?.Data ?? new List<VehicleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available vehicles from VehicleService");
            return new List<VehicleDto>();
        }
    }

    // Internal DTOs to deserialize the VehicleService response
    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    private class VehicleDto
    {
        public int VehicleId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public double CapacityKg { get; set; }
        public double CapacityM3 { get; set; }
        public double FuelEfficiencyKmPerLitre { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
