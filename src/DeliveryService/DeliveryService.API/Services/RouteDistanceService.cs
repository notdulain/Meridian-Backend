using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace DeliveryService.API.Services;

public partial class RouteDistanceService : IRouteDistanceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RouteDistanceService> _logger;

    public RouteDistanceService(HttpClient httpClient, ILogger<RouteDistanceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<double?> GetDistanceInKilometersAsync(string origin, string destination, CancellationToken cancellationToken = default)
    {
        var uri = $"/api/routes/calculate?origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}";
        using var response = await _httpClient.GetAsync(uri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No route found between {Origin} and {Destination}", origin, destination);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"RouteService returned {(int)response.StatusCode} while calculating distance. Response: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<RouteDistanceResponse>(cancellationToken: cancellationToken);
        return TryParseDistanceKm(payload?.Distance, out var distanceKm) ? distanceKm : null;
    }

    internal static bool TryParseDistanceKm(string? distanceText, out double distanceKm)
    {
        distanceKm = 0;
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return false;
        }

        var match = DistanceRegex().Match(distanceText.Trim());
        if (!match.Success || !double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var unit = match.Groups["unit"].Value.ToLowerInvariant();
        distanceKm = unit switch
        {
            "m" => value / 1000d,
            "km" => value,
            _ => 0
        };

        return distanceKm > 0;
    }

    [GeneratedRegex(@"^(?<value>\d+(?:\.\d+)?)\s*(?<unit>km|m)$", RegexOptions.IgnoreCase)]
    private static partial Regex DistanceRegex();

    private sealed class RouteDistanceResponse
    {
        public string? Distance { get; set; }
        public string? Duration { get; set; }
        public string? Polyline { get; set; }
    }
}
