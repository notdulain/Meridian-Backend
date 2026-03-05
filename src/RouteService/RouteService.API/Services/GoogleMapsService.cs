using System.Net.Http.Json;
using System.Text.Json;
using RouteService.API.Models;

namespace RouteService.API.Services;

public sealed class GoogleMapsService : IGoogleMapsService
{
    private const string RequestFailedMessage = "Unable to retrieve route information from Google Maps.";
    private const double FuelConsumptionRate = 0.08;
    private const double FuelPrice = 420;

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
        var routeResponse = await GetDirectionsResponseAsync(origin, destination, includeAlternatives: false, cancellationToken);

        var route = routeResponse.Routes.FirstOrDefault();
        var leg = route?.Legs.FirstOrDefault();

        if (route is null || leg?.Distance is null || leg.Duration is null || string.IsNullOrWhiteSpace(route.OverviewPolyline?.Points))
        {
            _logger.LogWarning(
                "Google Maps API returned an invalid or incomplete route. Origin: {Origin}, Destination: {Destination}, ErrorMessage: {ErrorMessage}",
                origin,
                destination,
                "Missing distance, duration, or polyline.");
            throw new GoogleMapsServiceException("Google Maps returned an incomplete route response.");
        }

        _logger.LogInformation(
            "Google Maps route lookup succeeded. Origin: {Origin}, Destination: {Destination}, Distance: {Distance}, Duration: {Duration}",
            origin,
            destination,
            leg.Distance.Text,
            leg.Duration.Text);

        return new GoogleDirectionsResult
        {
            Distance = leg.Distance.Text,
            Duration = leg.Duration.Text,
            Polyline = route.OverviewPolyline.Points
        };
    }

    public async Task<List<RouteOption>> GetAlternativeRoutesAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken)
    {
        var routeResponse = await GetDirectionsResponseAsync(origin, destination, includeAlternatives: true, cancellationToken);

        var parsedRoutes = new List<RouteOption>();

        for (var i = 0; i < routeResponse.Routes.Count; i++)
        {
            var route = routeResponse.Routes[i];
            var leg = route.Legs.FirstOrDefault();

            if (leg?.Distance is null || leg.Duration is null || string.IsNullOrWhiteSpace(route.OverviewPolyline?.Points))
            {
                _logger.LogWarning(
                    "Skipping route due to parsing errors. Origin: {Origin}, Destination: {Destination}, RouteIndex: {RouteIndex}, ErrorMessage: {ErrorMessage}",
                    origin,
                    destination,
                    i,
                    "Missing distance, duration, or polyline.");
                continue;
            }

            var fuelCost = CalculateFuelCost(leg.Distance.Value);

            var option = new RouteOption
            {
                RouteId = Guid.NewGuid().ToString("N"),
                Summary = string.IsNullOrWhiteSpace(route.Summary)
                    ? i == 0 ? "Optimized route" : $"Alternative route {i}"
                    : route.Summary,
                Distance = leg.Distance.Text,
                DistanceValue = leg.Distance.Value,
                Duration = leg.Duration.Text,
                DurationValue = leg.Duration.Value,
                FuelCost = fuelCost,
                PolylinePoints = route.OverviewPolyline!.Points
            };

            parsedRoutes.Add(option);
        }

        if (parsedRoutes.Count == 0)
        {
            _logger.LogWarning(
                "Google Maps API returned routes, but none could be parsed. Origin: {Origin}, Destination: {Destination}",
                origin,
                destination);
            throw new GoogleMapsServiceException("Google Maps returned routes that could not be parsed.");
        }

        var rankedRoutes = RankRoutes(parsedRoutes);

        _logger.LogInformation(
            "Google Maps alternatives lookup succeeded. Origin: {Origin}, Destination: {Destination}, RouteCount: {RouteCount}",
            origin,
            destination,
            rankedRoutes.Count);

        return rankedRoutes;
    }

    private static double CalculateFuelCost(int distanceMeters)
    {
        var distanceKm = distanceMeters / 1000d;
        return Math.Round(distanceKm * FuelConsumptionRate * FuelPrice, 2);
    }

    private static List<RouteOption> RankRoutes(IEnumerable<RouteOption> routes)
    {
        return routes
            .OrderBy(x => x.DurationValue)
            .ThenBy(x => x.DistanceValue)
            .ThenBy(x => x.FuelCost)
            .ToList();
    }

    private async Task<GoogleRouteResponse> GetDirectionsResponseAsync(
        string origin,
        string destination,
        bool includeAlternatives,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["GoogleMaps:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Google Maps API key is missing. Origin: {Origin}, Destination: {Destination}", origin, destination);
            throw new GoogleMapsServiceException("Google Maps API key is not configured.");
        }

        var requestUri =
            $"/maps/api/directions/json?origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&alternatives={includeAlternatives.ToString().ToLowerInvariant()}&key={Uri.EscapeDataString(apiKey)}";

        _logger.LogInformation(
            "Calling Google Maps Directions API. Origin: {Origin}, Destination: {Destination}, Alternatives: {Alternatives}",
            origin,
            destination,
            includeAlternatives);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Google Maps API request failed. Origin: {Origin}, Destination: {Destination}, StatusCode: {StatusCode}, ErrorMessage: {ErrorMessage}",
                origin,
                destination,
                ex.StatusCode,
                ex.Message);
            throw new GoogleMapsServiceException(RequestFailedMessage);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Google Maps API returned a non-success response. Origin: {Origin}, Destination: {Destination}, StatusCode: {StatusCode}, ErrorMessage: {ErrorMessage}",
                    origin,
                    destination,
                    response.StatusCode,
                    string.IsNullOrWhiteSpace(errorBody) ? "No response body returned." : errorBody);
                throw new GoogleMapsServiceException(RequestFailedMessage);
            }

            GoogleRouteResponse? routeResponse;
            try
            {
                routeResponse = await response.Content.ReadFromJsonAsync<GoogleRouteResponse>(SerializerOptions, cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Google Maps API JSON parsing failed. Origin: {Origin}, Destination: {Destination}, StatusCode: {StatusCode}, ErrorMessage: {ErrorMessage}",
                    origin,
                    destination,
                    response.StatusCode,
                    ex.Message);
                throw new GoogleMapsServiceException("Google Maps returned an invalid response.");
            }

            if (routeResponse is null)
            {
                _logger.LogWarning(
                    "Google Maps API returned an empty route response. Origin: {Origin}, Destination: {Destination}, StatusCode: {StatusCode}, ErrorMessage: {ErrorMessage}",
                    origin,
                    destination,
                    response.StatusCode,
                    "Response body could not be deserialized.");
                throw new GoogleMapsServiceException("Google Maps returned an invalid response.");
            }

            if (string.Equals(routeResponse.Status, "ZERO_RESULTS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Google Maps API returned no routes. Origin: {Origin}, Destination: {Destination}, StatusCode: {StatusCode}, ErrorMessage: {ErrorMessage}",
                    origin,
                    destination,
                    response.StatusCode,
                    routeResponse.ErrorMessage ?? routeResponse.Status);
                throw new RouteNotFoundException("No routes found for the provided origin and destination.");
            }

            if (!string.Equals(routeResponse.Status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                var errorMessage = string.IsNullOrWhiteSpace(routeResponse.ErrorMessage)
                    ? $"Google Directions API returned status '{routeResponse.Status}'."
                    : routeResponse.ErrorMessage;

                _logger.LogWarning(
                    "Google Maps API returned a non-success directions status. Origin: {Origin}, Destination: {Destination}, StatusCode: {StatusCode}, ErrorMessage: {ErrorMessage}",
                    origin,
                    destination,
                    response.StatusCode,
                    errorMessage);
                throw new GoogleMapsServiceException(RequestFailedMessage);
            }

            return routeResponse;
        }
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
