namespace RouteService.API.Models;

public class OptimizeRouteRequest
{
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public int VehicleId { get; set; }
    public int DeliveryId { get; set; }
}

public class RouteOption
{
    public required string RouteId { get; set; }
    public required string Summary { get; set; }
    public required string Distance { get; set; }
    public int DistanceValue { get; set; }
    public required string Duration { get; set; }
    public int DurationValue { get; set; }
    public double FuelCost { get; set; }
    public required string PolylinePoints { get; set; }
}
