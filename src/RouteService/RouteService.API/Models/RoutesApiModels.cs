using System.Text.Json.Serialization;

namespace RouteService.API.Models;

/// <summary>
/// Request body for POST https://routes.googleapis.com/directions/v2:computeRoutes
/// </summary>
public sealed class ComputeRoutesRequest
{
    [JsonPropertyName("origin")]
    public WaypointRequest Origin { get; set; } = null!;

    [JsonPropertyName("destination")]
    public WaypointRequest Destination { get; set; } = null!;

    [JsonPropertyName("travelMode")]
    public string TravelMode { get; set; } = "DRIVE";

    [JsonPropertyName("computeAlternativeRoutes")]
    public bool ComputeAlternativeRoutes { get; set; }
}

public sealed class WaypointRequest
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("location")]
    public WaypointLocationRequest? Location { get; set; }
}

public sealed class WaypointLocationRequest
{
    [JsonPropertyName("latLng")]
    public LatLngRequest? LatLng { get; set; }
}

public sealed class LatLngRequest
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

/// <summary>
/// Response from Routes API v2 computeRoutes.
/// Field mask: routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline,routes.routeLabels
/// </summary>
public sealed class ComputeRoutesResponse
{
    [JsonPropertyName("routes")]
    public List<RoutesApiRoute> Routes { get; set; } = [];

    [JsonPropertyName("error")]
    public RoutesApiError? Error { get; set; }
}

public sealed class RoutesApiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class RoutesApiRoute
{
    [JsonPropertyName("distanceMeters")]
    public int DistanceMeters { get; set; }

    /// <summary>Duration in protobuf format, e.g. "8100s" for 8100 seconds.</summary>
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("polyline")]
    public RoutesApiPolyline? Polyline { get; set; }

    [JsonPropertyName("routeLabels")]
    public List<string>? RouteLabels { get; set; }
}

public sealed class RoutesApiPolyline
{
    [JsonPropertyName("encodedPolyline")]
    public string? EncodedPolyline { get; set; }
}
