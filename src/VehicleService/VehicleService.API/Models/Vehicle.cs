namespace VehicleService.API.Models;

public class Vehicle
{
    public int VehicleId { get; set; }
    public required string PlateNumber { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public string CurrentLocation { get; set; } = string.Empty;
    public int Year { get; set; }
    public double CapacityKg { get; set; }
    public double CapacityM3 { get; set; }
    public double FuelEfficiencyKmPerLitre { get; set; }
    
    // Status enum: Available, OnTrip, Maintenance, Retired
    public required string Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
