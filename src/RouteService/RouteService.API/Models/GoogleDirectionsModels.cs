using System.Text.Json.Serialization;

namespace RouteService.API.Models;

public sealed class GoogleRouteResponse
{
    [JsonPropertyName("routes")]
    public List<Route> Routes { get; set; } = [];

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

public sealed class Route
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("legs")]
    public List<Leg> Legs { get; set; } = [];

    [JsonPropertyName("overview_polyline")]
    public Polyline? OverviewPolyline { get; set; }
}

public sealed class Leg
{
    [JsonPropertyName("distance")]
    public Distance? Distance { get; set; }

    [JsonPropertyName("duration")]
    public Duration? Duration { get; set; }
}

public sealed class Distance
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public sealed class Duration
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public sealed class Polyline
{
    [JsonPropertyName("points")]
    public string Points { get; set; } = string.Empty;
}
