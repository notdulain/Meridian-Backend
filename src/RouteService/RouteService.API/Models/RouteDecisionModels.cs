namespace RouteService.API.Models;

public sealed class SelectRouteRequest
{
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public int? VehicleId { get; set; }
    public int? DriverId { get; set; }
    public required RouteOption Route { get; set; }
}

public sealed class HistoryRouteDto
{
    public Guid RouteId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int? VehicleId { get; set; }
    public int? DriverId { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public decimal FuelCostLkr { get; set; }
    public decimal FuelConsumptionLitres { get; set; }
    public string Polyline { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SuggestedRouteDto
{
    public string RouteId { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public decimal FuelCostLkr { get; set; }
    public string Polyline { get; set; } = string.Empty;
}

public sealed class ComparisonRouteItem
{
    public string RouteId { get; set; } = string.Empty;
    public bool IsHistorical { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public decimal FuelCostLkr { get; set; }
    public decimal RankScore { get; set; }
}

public sealed class CompareRoutesResponse
{
    public List<HistoryRouteDto> HistoryRoutes { get; set; } = [];
    public List<SuggestedRouteDto> SuggestedRoutes { get; set; } = [];
    public List<ComparisonRouteItem> Comparison { get; set; } = [];
}
