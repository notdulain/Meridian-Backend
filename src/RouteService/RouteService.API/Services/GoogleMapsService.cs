using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RouteService.API.Models;

namespace RouteService.API.Services;

public sealed class GoogleMapsService : IGoogleMapsService
{
    private const string RequestFailedMessage = "Unable to retrieve route information from Google Maps.";
    private const string RoutesApiUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";
    private const string FieldMask = "routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline,routes.routeLabels";

    /// <summary>Placeholder: km per liter when vehicle info is unavailable.</summary>
    private const double DefaultVehicleFuelEfficiencyKmPerLiter = 12;

    /// <summary>Placeholder: fuel price in LKR when vehicle info is unavailable.</summary>
    private const double DefaultFuelPriceLkr = 450;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Regex DurationSecondsRegex = new(@"^(\d+(?:\.\d+)?)s$", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleMapsService> _logger;

    public GoogleMapsService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleMapsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GoogleDirectionsResult> GetRouteAsync(string origin, string destination, CancellationToken cancellationToken = default)
    {
        var routes = await ComputeRoutesAsync(origin, destination, includeAlternatives: false, cancellationToken);

        if (routes.Count == 0)
        {
            _logger.LogWarning("Google Routes API returned no routes. Origin: {Origin}, Destination: {Destination}", origin, destination);
            throw new RouteNotFoundException("No routes found for the provided origin and destination.");
        }

        var route = routes[0];
        _logger.LogInformation(
            "Google Routes API route lookup succeeded. Origin: {Origin}, Destination: {Destination}, Distance: {Distance}, Duration: {Duration}",
            origin, destination, route.Distance, route.Duration);

        return new GoogleDirectionsResult
        {
            Distance = route.Distance,
            Duration = route.Duration,
            Polyline = route.PolylinePoints
        };
    }

    public async Task<List<RouteOption>> GetAlternativeRoutesAsync(string origin, string destination, CancellationToken cancellationToken)
    {
        var routes = await ComputeRoutesAsync(origin, destination, includeAlternatives: true, cancellationToken);

        if (routes.Count == 0)
        {
            _logger.LogWarning("Google Routes API returned no routes. Origin: {Origin}, Destination: {Destination}", origin, destination);
            throw new RouteNotFoundException("No routes found for the provided origin and destination.");
        }

        var ranked = RankRoutes(routes);
        _logger.LogInformation(
            "Google Routes API alternatives lookup succeeded. Origin: {Origin}, Destination: {Destination}, RouteCount: {RouteCount}",
            origin, destination, ranked.Count);

        return ranked;
    }

    private async Task<List<RouteOption>> ComputeRoutesAsync(string origin, string destination, bool includeAlternatives, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            throw new GoogleMapsServiceException("Origin and destination must not be null or empty.");

        var apiKey = _configuration["GoogleMaps:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Google Maps API key is missing. Origin: {Origin}, Destination: {Destination}", origin, destination);
            throw new GoogleMapsServiceException("Google Maps API key is not configured.");
        }

        var requestBody = BuildRequestBody(origin, destination, includeAlternatives);
        var json = JsonSerializer.Serialize(requestBody, SerializerOptions);

        _logger.LogInformation(
            "Google Routes API request. Origin: {Origin}, Destination: {Destination}, ComputeAlternativeRoutes: {Alternatives}",
            origin, destination, includeAlternatives);

        using var request = new HttpRequestMessage(HttpMethod.Post, RoutesApiUrl);
        request.Headers.Add("X-Goog-Api-Key", apiKey);
        request.Headers.Add("X-Goog-FieldMask", FieldMask);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Google Routes API request failed. Origin: {Origin}, Destination: {Destination}, Error: {Error}", origin, destination, ex.Message);
            throw new GoogleMapsServiceException($"{RequestFailedMessage} {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Google Routes API request timed out. Origin: {Origin}, Destination: {Destination}", origin, destination);
            throw new GoogleMapsServiceException($"{RequestFailedMessage} Request timed out.");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Google Routes API non-success HTTP. Origin: {Origin}, Destination: {Destination}, StatusCode: {StatusCode}, Response: {Response}",
                origin, destination, response.StatusCode, string.IsNullOrWhiteSpace(responseContent) ? "(empty)" : responseContent);
            var message = string.IsNullOrWhiteSpace(responseContent) ? $"{RequestFailedMessage} HTTP {(int)response.StatusCode}." : $"{RequestFailedMessage} {responseContent}";
            throw new GoogleMapsServiceException(message);
        }

        _logger.LogInformation("Google Routes API response: {Response}",
            responseContent.Length > 500 ? responseContent[..500] + "..." : responseContent);

        ComputeRoutesResponse? apiResponse;
        try
        {
            apiResponse = JsonSerializer.Deserialize<ComputeRoutesResponse>(responseContent, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Google Routes API JSON parse failed. Origin: {Origin}, Destination: {Destination}, Raw: {Raw}",
                origin, destination, responseContent.Length > 1000 ? responseContent[..1000] + "..." : responseContent);
            throw new GoogleMapsServiceException($"Google Maps returned an invalid response. {ex.Message}");
        }

        if (apiResponse is null)
        {
            _logger.LogWarning("Google Routes API returned null body. Origin: {Origin}, Destination: {Destination}", origin, destination);
            throw new GoogleMapsServiceException("Google Maps returned an invalid response.");
        }

        if (apiResponse.Error is not null)
        {
            var errMsg = apiResponse.Error.Message ?? apiResponse.Error.Status ?? "Unknown error";
            _logger.LogWarning("Google Routes API error. Origin: {Origin}, Destination: {Destination}, Code: {Code}, Message: {Message}",
                origin, destination, apiResponse.Error.Code, errMsg);
            throw new GoogleMapsServiceException(errMsg);
        }

        if (apiResponse.Routes is null || apiResponse.Routes.Count == 0)
        {
            _logger.LogWarning("Google Routes API returned no routes. Origin: {Origin}, Destination: {Destination}", origin, destination);
            throw new RouteNotFoundException("No routes found for the provided origin and destination.");
        }

        var options = new List<RouteOption>();
        for (var i = 0; i < apiResponse.Routes.Count; i++)
        {
            var r = apiResponse.Routes[i];
            var durationSeconds = ParseDurationSeconds(r.Duration);
            var distanceKm = r.DistanceMeters / 1000.0;
            var fuelCost = CalculateFuelCost(r.DistanceMeters);

            var summary = i == 0 ? "Primary Route" : "Alternative Route";
            if (r.RouteLabels is { Count: > 0 })
            {
                var label = r.RouteLabels.FirstOrDefault(l =>
                    string.Equals(l, "DEFAULT_ROUTE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(l, "DEFAULT_ROUTE_ALTERNATE", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(label))
                    summary = label == "DEFAULT_ROUTE" ? "Primary Route" : "Alternative Route";
            }

            options.Add(new RouteOption
            {
                RouteId = Guid.NewGuid().ToString("N"),
                Summary = summary,
                Distance = FormatDistanceKm(distanceKm),
                DistanceValue = r.DistanceMeters,
                Duration = FormatDurationReadable(durationSeconds),
                DurationValue = durationSeconds,
                FuelCost = fuelCost,
                PolylinePoints = r.Polyline?.EncodedPolyline ?? string.Empty
            });
        }

        return options;
    }

    private static ComputeRoutesRequest BuildRequestBody(string origin, string destination, bool computeAlternativeRoutes)
    {
        return new ComputeRoutesRequest
        {
            Origin = CreateWaypoint(origin),
            Destination = CreateWaypoint(destination),
            TravelMode = "DRIVE",
            ComputeAlternativeRoutes = computeAlternativeRoutes
        };
    }

    private static WaypointRequest CreateWaypoint(string value)
    {
        if (TryParseLatLng(value, out var lat, out var lng))
        {
            return new WaypointRequest
            {
                Location = new WaypointLocationRequest
                {
                    LatLng = new LatLngRequest { Latitude = lat, Longitude = lng }
                }
            };
        }
        return new WaypointRequest { Address = value.Trim() };
    }

    private static bool TryParseLatLng(string value, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return false;
        return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
               && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
    }

    private static int ParseDurationSeconds(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration)) return 0;
        var m = DurationSecondsRegex.Match(duration.Trim());
        if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
            return (int)Math.Round(sec);
        return 0;
    }

    private static string FormatDistanceKm(double km)
    {
        if (km < 1)
            return $"{(int)Math.Round(km * 1000)} m";
        return $"{km.ToString("F1", CultureInfo.InvariantCulture)} km";
    }

    private static string FormatDurationReadable(int totalSeconds)
    {
        if (totalSeconds < 60)
            return $"{totalSeconds} sec";
        var minutes = totalSeconds / 60;
        if (minutes < 60)
            return $"{minutes} min";
        var hours = minutes / 60;
        minutes %= 60;
        return minutes > 0 ? $"{hours} hr {minutes} min" : $"{hours} hr";
    }

    private static double CalculateFuelCost(int distanceMeters)
    {
        var distanceKm = distanceMeters / 1000.0;
        return Math.Round((distanceKm / DefaultVehicleFuelEfficiencyKmPerLiter) * DefaultFuelPriceLkr, 2);
    }

    private static List<RouteOption> RankRoutes(List<RouteOption> routes)
    {
        return routes
            .OrderBy(x => x.DurationValue)
            .ThenBy(x => x.DistanceValue)
            .ThenBy(x => x.FuelCost)
            .ToList();
    }
}

public sealed class GoogleDirectionsResult
{
    public required string Distance { get; init; }
    public required string Duration { get; init; }
    public required string Polyline { get; init; }
}

public sealed class GoogleMapsServiceException : Exception
{
    public GoogleMapsServiceException(string message) : base(message) { }
}

public sealed class RouteNotFoundException : Exception
{
    public RouteNotFoundException(string message) : base(message) { }
}
