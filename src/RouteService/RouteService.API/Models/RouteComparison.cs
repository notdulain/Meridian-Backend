namespace RouteService.API.Models;

/// <summary>
/// Represents a route option from the Google Routes API for dispatcher comparison (MER-67).
/// </summary>
public class RouteComparison
{
    public required string RouteId { get; set; }
    public required string Summary { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public double FuelCost { get; set; }
    public bool IsRecommended { get; set; }
    public required string PolylinePoints { get; set; }
}

/// <summary>
/// Response for the route comparison endpoint containing all options and the recommended route id.
/// </summary>
public class RouteComparisonResponse
{
    public bool Success { get; set; }
    public required List<RouteComparison> Routes { get; set; }
    public required string RecommendedRouteId { get; set; }
}
