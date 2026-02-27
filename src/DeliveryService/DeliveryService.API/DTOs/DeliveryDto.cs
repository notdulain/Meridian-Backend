using System;

namespace DeliveryService.API.DTOs;

public class CreateDeliveryRequestDto
{
    public string PickupAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal PackageWeightKg { get; set; }
    public decimal PackageVolumeM3 { get; set; }
    public DateTime Deadline { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateDeliveryRequestDto
{
    public string? PickupAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public decimal? PackageWeightKg { get; set; }
    public decimal? PackageVolumeM3 { get; set; }
    public DateTime? Deadline { get; set; }
}

public class DeliveryDto
{
    public int Id { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal PackageWeightKg { get; set; }
    public decimal PackageVolumeM3 { get; set; }
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? AssignedVehicleId { get; set; }
    public int? AssignedDriverId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<DeliveryStatusHistoryDto> StatusHistory { get; set; } = [];
}

public class DeliveryStatusHistoryDto
{
    public int StatusHistoryId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public int ChangedBy { get; set; }
    public string? Notes { get; set; }
}
