namespace RouteService.API.Models;

/// <summary>
/// Represents one ranked route option for dispatcher selection (fuel consumption in litres, fuel cost in LKR, duration in hours).
/// </summary>
public class RouteRankedOption
{
    public required string RouteId { get; set; }
    public int Rank { get; set; }
    public required string Summary { get; set; }
    public double DistanceKm { get; set; }
    public double DurationHours { get; set; }
    public double FuelConsumptionLitres { get; set; }
    public double FuelCostLKR { get; set; }
    public required string PolylinePoints { get; set; }
    public bool IsRecommended { get; set; }
}

/// <summary>
/// Response for the route ranking endpoint: all route options in ranked order with recommended route id.
/// </summary>
public class RouteRankingResponse
{
    public bool Success { get; set; }
    public required List<RouteRankedOption> Routes { get; set; }
    public required string RecommendedRouteId { get; set; }
}
