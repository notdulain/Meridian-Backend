using System;

namespace DeliveryService.API.DTOs;

public class DeliveryDto
{
    public int Id { get; set; }
    public string? OrderNumber { get; set; }
    public string? Status { get; set; }
    public string? Destination { get; set; }
    public DateTime CreatedAt { get; set; }
}
