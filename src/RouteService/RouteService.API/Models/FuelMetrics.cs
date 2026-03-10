namespace RouteService.API.Models;

public sealed class FuelMetrics
{
    public double DistanceKm { get; init; }
    public double FuelConsumptionLitres { get; init; }
    public double FuelCostLKR { get; init; }
}
