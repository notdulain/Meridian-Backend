using System;

namespace DeliveryService.API.Models;

public class Delivery
{
    public int Id { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal PackageWeightKg { get; set; }
    public decimal PackageVolumeM3 { get; set; }
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = "Pending";
    public int? AssignedVehicleId { get; set; }
    public int? AssignedDriverId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? OrderNumber { get; set; }
    public string? Status { get; set; }
    public string? Destination { get; set; }
    public DateTime CreatedAt { get; set; }
}
