namespace RouteService.API.Models;

public sealed class FuelCostAggregate
{
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public int TripCount { get; set; }
    public double TotalDistanceKm { get; set; }
    public decimal TotalFuelConsumptionLitres { get; set; }
    public decimal TotalFuelCostLkr { get; set; }
}