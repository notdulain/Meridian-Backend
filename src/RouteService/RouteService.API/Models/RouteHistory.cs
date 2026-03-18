using System.ComponentModel.DataAnnotations;

namespace RouteService.API.Models;

public sealed class RouteHistory
{
    [Key]
    public Guid RouteId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Destination { get; set; } = string.Empty;

    public double DistanceKm { get; set; }

    public int DurationMinutes { get; set; }

    public decimal FuelCostLkr { get; set; }

    public decimal FuelConsumptionLitres { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Polyline { get; set; } = string.Empty;

    public bool Selected { get; set; }

    public DateTime CreatedAt { get; set; }
}
