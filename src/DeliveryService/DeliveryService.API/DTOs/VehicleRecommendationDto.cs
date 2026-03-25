using System;

namespace DeliveryService.API.DTOs;

public class VehicleRecommendationDto
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double CapacityKg { get; set; }
    public double CapacityM3 { get; set; }
    public double FuelEfficiencyKmPerLitre { get; set; }
    public string CurrentLocation { get; set; } = string.Empty;
    public double? DistanceToPickupKm { get; set; }

    // Additional fields for recommendation context
    public double MatchScore { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
}
