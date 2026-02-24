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

public class DeliveryDto
{
    public int Id { get; set; }
}
