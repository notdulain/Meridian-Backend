using System.Text.Json;
using System.Net.Http.Json;
using RouteService.API.Models;

namespace RouteService.API.Services;

public interface IGoogleMapsService
{
    Task<GoogleDirectionsResult> GetRouteAsync(string origin, string destination, CancellationToken cancellationToken = default);
}

public sealed class GoogleMapsService : IGoogleMapsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        var apiKey = _configuration["GoogleMaps:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new GoogleMapsServiceException("Google Maps API key is not configured.");
        }

        var requestUri =
            $"/maps/api/directions/json?origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&key={Uri.EscapeDataString(apiKey)}";

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google Directions API returned HTTP {StatusCode} for origin '{Origin}' and destination '{Destination}'.",
                response.StatusCode, origin, destination);
            throw new GoogleMapsServiceException("Google Directions API request failed.");
        }

        GoogleRouteResponse? routeResponse;
        try
        {
            routeResponse = await response.Content.ReadFromJsonAsync<GoogleRouteResponse>(SerializerOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Google Directions API returned malformed JSON.");
            throw new GoogleMapsServiceException("Google Directions API returned an invalid response.");
        }

        if (routeResponse is null)
        {
            throw new GoogleMapsServiceException("Google Directions API returned an invalid response.");
        }

        if (string.Equals(routeResponse.Status, "ZERO_RESULTS", StringComparison.OrdinalIgnoreCase))
        {
            throw new RouteNotFoundException("No routes found for the provided origin and destination.");
        }

        if (!string.Equals(routeResponse.Status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = string.IsNullOrWhiteSpace(routeResponse.ErrorMessage)
                ? $"Google Directions API returned status '{routeResponse.Status}'."
                : routeResponse.ErrorMessage;

            _logger.LogWarning("Google Directions API returned status '{Status}' for origin '{Origin}' and destination '{Destination}'.",
                routeResponse.Status, origin, destination);
            throw new GoogleMapsServiceException(errorMessage);
        }

        var route = routeResponse.Routes.FirstOrDefault();
        var leg = route?.Legs.FirstOrDefault();

        if (route is null || leg?.Distance is null || leg.Duration is null || string.IsNullOrWhiteSpace(route.OverviewPolyline?.Points))
        {
            throw new GoogleMapsServiceException("Google Directions API response is missing route details.");
        }

        return new GoogleDirectionsResult
        {
            Distance = leg.Distance.Text,
            Duration = leg.Duration.Text,
            Polyline = route.OverviewPolyline.Points
        };
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
    public GoogleMapsServiceException(string message) : base(message)
    {
    }
}

public sealed class RouteNotFoundException : Exception
{
    public RouteNotFoundException(string message) : base(message)
    {
    }
}
