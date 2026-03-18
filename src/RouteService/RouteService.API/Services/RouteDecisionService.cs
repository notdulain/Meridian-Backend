using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Distributed;
using RouteService.API.Models;
using RouteService.API.Repositories;

namespace RouteService.API.Services;

public sealed class RouteDecisionService : IRouteDecisionService
{
    private const decimal DistanceWeight = 0.4m;
    private const decimal DurationWeight = 0.4m;
    private const decimal FuelCostWeight = 0.2m;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private static readonly Regex HourRegex = new(@"(\d+)\s*hr", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MinuteRegex = new(@"(\d+)\s*min", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IRouteHistoryRepository _routeHistoryRepository;
    private readonly IGoogleMapsService _googleMapsService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RouteDecisionService> _logger;
    private readonly IConfiguration _configuration;

    public RouteDecisionService(
        IRouteHistoryRepository routeHistoryRepository,
        IGoogleMapsService googleMapsService,
        IDistributedCache cache,
        ILogger<RouteDecisionService> logger,
        IConfiguration configuration)
    {
        _routeHistoryRepository = routeHistoryRepository;
        _googleMapsService = googleMapsService;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HistoryRouteDto> SaveSelectedRouteAsync(SelectRouteRequest request, CancellationToken cancellationToken)
    {
        var distanceKm = ResolveDistanceKm(request.Route);
        var durationMinutes = ResolveDurationMinutes(request.Route);
        var fuelCost = request.Route.FuelCost > 0
            ? Convert.ToDecimal(request.Route.FuelCost, CultureInfo.InvariantCulture)
            : CalculateFuelCost(distanceKm);
        var fuelConsumption = CalculateFuelConsumption(distanceKm);

        var historyRoute = new RouteHistory
        {
            RouteId = Guid.NewGuid(),
            Origin = request.Origin.Trim(),
            Destination = request.Destination.Trim(),
            DistanceKm = distanceKm,
            DurationMinutes = durationMinutes,
            FuelCostLkr = Math.Round(fuelCost, 2, MidpointRounding.AwayFromZero),
            FuelConsumptionLitres = Math.Round(fuelConsumption, 2, MidpointRounding.AwayFromZero),
            Polyline = request.Route.PolylinePoints ?? string.Empty,
            Selected = true,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _routeHistoryRepository.AddAsync(historyRoute, cancellationToken);
        await InvalidateHistoryCacheAsync(saved.Origin, saved.Destination, cancellationToken);

        _logger.LogInformation(
            "Route selected and stored. RouteId: {RouteId}, Origin: {Origin}, Destination: {Destination}, DistanceKm: {DistanceKm}, DurationMinutes: {DurationMinutes}",
            saved.RouteId, saved.Origin, saved.Destination, saved.DistanceKm, saved.DurationMinutes);

        return MapHistory(saved);
    }

    public async Task<IReadOnlyList<HistoryRouteDto>> GetHistoryAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildHistoryCacheKey(origin, destination);
        var cached = await TryGetCachedValueAsync<List<HistoryRouteDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var history = await _routeHistoryRepository.GetByOriginDestinationAsync(origin, destination, cancellationToken);
        var response = history.Select(MapHistory).ToList();

        await SetCachedValueAsync(cacheKey, response, cancellationToken);
        return response;
    }

    public async Task<CompareRoutesResponse> CompareRoutesAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken)
    {
        var history = (await GetHistoryAsync(origin, destination, cancellationToken)).ToList();
        var suggested = await GetSuggestedRoutesWithFallbackAsync(origin, destination, cancellationToken);

        var comparison = BuildComparison(history, suggested);

        return new CompareRoutesResponse
        {
            HistoryRoutes = history,
            SuggestedRoutes = suggested,
            Comparison = comparison
        };
    }

    private async Task<List<SuggestedRouteDto>> GetSuggestedRoutesWithFallbackAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCurrentRoutesCacheKey(origin, destination);
        var cached = await TryGetCachedValueAsync<List<SuggestedRouteDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        try
        {
            var routes = await _googleMapsService.GetAlternativeRoutesAsync(origin, destination, cancellationToken);
            var suggested = routes.Select(MapSuggested).ToList();
            await SetCachedValueAsync(cacheKey, suggested, cancellationToken);
            return suggested;
        }
        catch (RouteNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "No suggested routes found from Google Maps. Origin: {Origin}, Destination: {Destination}",
                origin,
                destination);
            return [];
        }
        catch (GoogleMapsServiceException ex)
        {
            _logger.LogError(
                ex,
                "Google Maps failed while fetching suggested routes. Falling back to route history. Origin: {Origin}, Destination: {Destination}",
                origin,
                destination);
            return [];
        }
    }

    private List<ComparisonRouteItem> BuildComparison(
        IReadOnlyCollection<HistoryRouteDto> historyRoutes,
        IReadOnlyCollection<SuggestedRouteDto> suggestedRoutes)
    {
        var combined = new List<ComparisonRouteItem>(historyRoutes.Count + suggestedRoutes.Count);

        foreach (var historyRoute in historyRoutes)
        {
            combined.Add(new ComparisonRouteItem
            {
                RouteId = historyRoute.RouteId.ToString(),
                IsHistorical = true,
                DistanceKm = historyRoute.DistanceKm,
                DurationMinutes = historyRoute.DurationMinutes,
                FuelCostLkr = historyRoute.FuelCostLkr,
                RankScore = CalculateRankScore(historyRoute.DistanceKm, historyRoute.DurationMinutes, historyRoute.FuelCostLkr)
            });
        }

        foreach (var suggestedRoute in suggestedRoutes)
        {
            combined.Add(new ComparisonRouteItem
            {
                RouteId = suggestedRoute.RouteId,
                IsHistorical = false,
                DistanceKm = suggestedRoute.DistanceKm,
                DurationMinutes = suggestedRoute.DurationMinutes,
                FuelCostLkr = suggestedRoute.FuelCostLkr,
                RankScore = CalculateRankScore(suggestedRoute.DistanceKm, suggestedRoute.DurationMinutes, suggestedRoute.FuelCostLkr)
            });
        }

        return combined
            .OrderBy(x => x.RankScore)
            .ThenBy(x => x.DistanceKm)
            .ThenBy(x => x.DurationMinutes)
            .ToList();
    }

    private static decimal CalculateRankScore(double distanceKm, int durationMinutes, decimal fuelCostLkr)
    {
        var distance = Convert.ToDecimal(distanceKm, CultureInfo.InvariantCulture);
        var duration = Convert.ToDecimal(durationMinutes, CultureInfo.InvariantCulture);
        var score = (distance * DistanceWeight) + (duration * DurationWeight) + (fuelCostLkr * FuelCostWeight);
        return Math.Round(score, 4, MidpointRounding.AwayFromZero);
    }

    private static HistoryRouteDto MapHistory(RouteHistory routeHistory)
    {
        return new HistoryRouteDto
        {
            RouteId = routeHistory.RouteId,
            Origin = routeHistory.Origin,
            Destination = routeHistory.Destination,
            DistanceKm = routeHistory.DistanceKm,
            DurationMinutes = routeHistory.DurationMinutes,
            FuelCostLkr = routeHistory.FuelCostLkr,
            FuelConsumptionLitres = routeHistory.FuelConsumptionLitres,
            Polyline = routeHistory.Polyline,
            Selected = routeHistory.Selected,
            CreatedAt = routeHistory.CreatedAt
        };
    }

    private SuggestedRouteDto MapSuggested(RouteOption routeOption)
    {
        var distanceKm = ResolveDistanceKm(routeOption);
        var durationMinutes = ResolveDurationMinutes(routeOption);
        var fuelCost = routeOption.FuelCost > 0
            ? Convert.ToDecimal(routeOption.FuelCost, CultureInfo.InvariantCulture)
            : CalculateFuelCost(distanceKm);

        return new SuggestedRouteDto
        {
            RouteId = routeOption.RouteId,
            Summary = routeOption.Summary,
            DistanceKm = distanceKm,
            DurationMinutes = durationMinutes,
            FuelCostLkr = Math.Round(fuelCost, 2, MidpointRounding.AwayFromZero),
            Polyline = routeOption.PolylinePoints
        };
    }

    private async Task<T?> TryGetCachedValueAsync<T>(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogInformation("Cache miss. Key: {CacheKey}", cacheKey);
                return default;
            }

            var value = JsonSerializer.Deserialize<T>(payload, SerializerOptions);
            if (value is null)
            {
                _logger.LogInformation("Cache miss due to deserialization result. Key: {CacheKey}", cacheKey);
                return default;
            }

            _logger.LogInformation("Cache hit. Key: {CacheKey}", cacheKey);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache read failed. Key: {CacheKey}", cacheKey);
            return default;
        }
    }

    private async Task SetCachedValueAsync<T>(string cacheKey, T value, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(value, SerializerOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl
                },
                cancellationToken);

            _logger.LogInformation("Cache write success. Key: {CacheKey}, TtlMinutes: {TtlMinutes}", cacheKey, CacheTtl.TotalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache write failed. Key: {CacheKey}", cacheKey);
        }
    }

    private async Task InvalidateHistoryCacheAsync(string origin, string destination, CancellationToken cancellationToken)
    {
        var historyCacheKey = BuildHistoryCacheKey(origin, destination);
        try
        {
            await _cache.RemoveAsync(historyCacheKey, cancellationToken);
            _logger.LogInformation("History cache invalidated. Key: {CacheKey}", historyCacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "History cache invalidation failed. Key: {CacheKey}", historyCacheKey);
        }
    }

    private double CalculateFuelConsumption(double distanceKm)
    {
        var fuelEfficiency = _configuration.GetValue<double?>("RouteOptimization:FuelEfficiencyKmPerLitre") ?? 12d;
        if (fuelEfficiency <= 0 || !double.IsFinite(fuelEfficiency))
            throw new InvalidOperationException("RouteOptimization:FuelEfficiencyKmPerLitre configuration is invalid.");

        return distanceKm / fuelEfficiency;
    }

    private decimal CalculateFuelCost(double distanceKm)
    {
        var fuelPrice = _configuration.GetValue<decimal?>("RouteOptimization:FuelPriceLkr") ?? 303m;
        if (fuelPrice < 0)
            throw new InvalidOperationException("RouteOptimization:FuelPriceLkr configuration is invalid.");

        var consumption = Convert.ToDecimal(CalculateFuelConsumption(distanceKm), CultureInfo.InvariantCulture);
        return consumption * fuelPrice;
    }

    private static int ResolveDurationMinutes(RouteOption routeOption)
    {
        if (routeOption.DurationValue > 0)
            return (int)Math.Round(routeOption.DurationValue / 60d, MidpointRounding.AwayFromZero);

        return ParseDurationMinutesFromText(routeOption.Duration);
    }

    private static double ResolveDistanceKm(RouteOption routeOption)
    {
        if (routeOption.DistanceValue > 0)
            return Math.Round(routeOption.DistanceValue / 1000d, 2, MidpointRounding.AwayFromZero);

        return TryParseDistanceFromText(routeOption.Distance);
    }

    private static double TryParseDistanceFromText(string? distance)
    {
        if (string.IsNullOrWhiteSpace(distance))
            return 0;

        var numericPart = new string(distance.Trim().TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        return double.TryParse(numericPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static int ParseDurationMinutesFromText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var text = value.Trim();
        var hourMatch = HourRegex.Match(text);
        var minuteMatch = MinuteRegex.Match(text);

        if (hourMatch.Success || minuteMatch.Success)
        {
            var hours = hourMatch.Success ? int.Parse(hourMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            var minutes = minuteMatch.Success ? int.Parse(minuteMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            return (hours * 60) + minutes;
        }

        var numericPart = new string(text.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(numericPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : 0;
    }

    private static string BuildHistoryCacheKey(string origin, string destination)
    {
        return $"route:history:{Normalize(origin)}:{Normalize(destination)}";
    }

    private static string BuildCurrentRoutesCacheKey(string origin, string destination)
    {
        return $"route:current:{Normalize(origin)}:{Normalize(destination)}";
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
