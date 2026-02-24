using System;

namespace DeliveryService.API.Models;

public class Delivery
{
    public int Id { get; set; }
    public string? OrderNumber { get; set; }
    public string? Status { get; set; }
    public string? Destination { get; set; }
    public DateTime CreatedAt { get; set; }
}
