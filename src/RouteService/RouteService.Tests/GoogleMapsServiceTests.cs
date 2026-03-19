using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RouteService.API.Services;
using Xunit;

namespace RouteService.Tests;

public class GoogleMapsServiceTests
{
    private const string FakeApiKey = "test-api-key";

    [Fact]
    public async Task GetRouteAsync_UsesMockGoogleResponse_AndParsesPrimaryRoute()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(CreateJsonResponse("""
            {
              "routes": [
                {
                  "distanceMeters": 115000,
                  "duration": "7200s",
                  "polyline": { "encodedPolyline": "encoded-polyline" },
                  "routeLabels": ["DEFAULT_ROUTE"]
                }
              ]
            }
            """)));

        var service = CreateService(handler);

        var result = await service.GetRouteAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.Equal("115.0 km", result.Distance);
        Assert.Equal("2 hr", result.Duration);
        Assert.Equal("encoded-polyline", result.Polyline);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetRouteAsync_SendsExpectedGoogleRoutesRequest()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(CreateJsonResponse("""
            {
              "routes": [
                {
                  "distanceMeters": 1000,
                  "duration": "60s",
                  "polyline": { "encodedPolyline": "p1" },
                  "routeLabels": ["DEFAULT_ROUTE"]
                }
              ]
            }
            """)));

        var service = CreateService(handler);

        await service.GetRouteAsync("6.9271,79.8612", "Kandy", CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("https://routes.googleapis.com/directions/v2:computeRoutes", handler.LastRequestUri?.ToString());
        Assert.Equal(FakeApiKey, Assert.Single(handler.LastHeaders["X-Goog-Api-Key"]));
        Assert.Contains("routes.distanceMeters", Assert.Single(handler.LastHeaders["X-Goog-FieldMask"]));

        using var doc = JsonDocument.Parse(handler.LastBody!);
        var root = doc.RootElement;
        Assert.Equal("DRIVE", root.GetProperty("travelMode").GetString());
        Assert.False(root.GetProperty("computeAlternativeRoutes").GetBoolean());
        Assert.Equal(6.9271, root.GetProperty("origin").GetProperty("location").GetProperty("latLng").GetProperty("latitude").GetDouble());
        Assert.Equal(79.8612, root.GetProperty("origin").GetProperty("location").GetProperty("latLng").GetProperty("longitude").GetDouble());
        Assert.Equal("Kandy", root.GetProperty("destination").GetProperty("address").GetString());
    }

    [Fact]
    public async Task GetRouteAsync_UsesCachedRoute_WhenAvailable()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(CreateJsonResponse("""
            {
              "routes": [
                {
                  "distanceMeters": 115000,
                  "duration": "7200s",
                  "polyline": { "encodedPolyline": "encoded-polyline" },
                  "routeLabels": ["DEFAULT_ROUTE"]
                }
              ]
            }
            """)));
        var cache = CreateCache();
        var service = CreateService(handler, cache: cache);

        var firstResult = await service.GetRouteAsync("Colombo", "Kandy", CancellationToken.None);
        var secondResult = await service.GetRouteAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.Equal("115.0 km", firstResult.Distance);
        Assert.Equal(firstResult.Distance, secondResult.Distance);
        Assert.Equal(firstResult.Duration, secondResult.Duration);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetAlternativeRoutesAsync_SendsAlternativeRoutesFlag_AndSortsResults()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(CreateJsonResponse("""
            {
              "routes": [
                {
                  "distanceMeters": 120000,
                  "duration": "8100s",
                  "polyline": { "encodedPolyline": "slow-route" },
                  "routeLabels": ["DEFAULT_ROUTE"]
                },
                {
                  "distanceMeters": 115000,
                  "duration": "7200s",
                  "polyline": { "encodedPolyline": "fast-route" },
                  "routeLabels": ["DEFAULT_ROUTE_ALTERNATE"]
                }
              ]
            }
            """)));
        var service = CreateService(handler);

        var result = await service.GetAlternativeRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("fast-route", result[0].PolylinePoints);
        Assert.Equal("115.0 km", result[0].Distance);
        Assert.Equal("2 hr", result[0].Duration);

        using var doc = JsonDocument.Parse(handler.LastBody!);
        Assert.True(doc.RootElement.GetProperty("computeAlternativeRoutes").GetBoolean());
    }

    [Fact]
    public async Task GetRouteComparisonsAsync_MarksLowestFuelCostRouteAsRecommended()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(CreateJsonResponse("""
            {
              "routes": [
                {
                  "distanceMeters": 120000,
                  "duration": "7200s",
                  "polyline": { "encodedPolyline": "route-a" },
                  "routeLabels": ["DEFAULT_ROUTE"]
                },
                {
                  "distanceMeters": 105000,
                  "duration": "7800s",
                  "polyline": { "encodedPolyline": "route-b" },
                  "routeLabels": ["DEFAULT_ROUTE_ALTERNATE"]
                }
              ]
            }
            """)));
        var service = CreateService(handler);

        var comparisons = await service.GetRouteComparisonsAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.Equal(2, comparisons.Count);
        var recommended = Assert.Single(comparisons, r => r.IsRecommended);
        Assert.Equal("route-b", recommended.PolylinePoints);
        Assert.Equal(105, recommended.DistanceKm);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_CalculatesFuelMetrics_AndAssignsRanks()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(CreateJsonResponse("""
            {
              "routes": [
                {
                  "distanceMeters": 120000,
                  "duration": "7200s",
                  "polyline": { "encodedPolyline": "route-a" },
                  "routeLabels": ["DEFAULT_ROUTE"]
                },
                {
                  "distanceMeters": 60000,
                  "duration": "3600s",
                  "polyline": { "encodedPolyline": "route-b" },
                  "routeLabels": ["DEFAULT_ROUTE_ALTERNATE"]
                }
              ]
            }
            """)));
        var service = CreateService(handler);

        var result = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Routes.Count);
        Assert.Equal(new[] { 1, 2 }, result.Routes.Select(r => r.Rank).ToArray());
        Assert.Equal("route-b", result.Routes[0].PolylinePoints);
        Assert.True(result.Routes[0].IsRecommended);
        Assert.Equal(result.Routes[0].RouteId, result.RecommendedRouteId);
        Assert.Equal(60, result.Routes[0].DistanceKm);
        Assert.Equal(5, result.Routes[0].FuelConsumptionLitres);
        Assert.Equal(1515, result.Routes[0].FuelCostLKR);
        Assert.Equal(120, result.Routes[1].DistanceKm);
        Assert.Equal(10, result.Routes[1].FuelConsumptionLitres);
        Assert.Equal(3030, result.Routes[1].FuelCostLKR);
    }

    private static GoogleMapsService CreateService(
        RecordingHttpMessageHandler handler,
        IDistributedCache? cache = null,
        Dictionary<string, string?>? overrides = null)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://routes.googleapis.com") };
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(f => f.CreateClient(nameof(GoogleMapsService))).Returns(client);

        var configValues = new Dictionary<string, string?>
        {
            ["GoogleMaps:ApiKey"] = FakeApiKey,
            ["RouteOptimization:FuelEfficiencyKmPerLitre"] = "12",
            ["RouteOptimization:FuelPriceLkr"] = "303"
        };

        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                configValues[pair.Key] = pair.Value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new GoogleMapsService(
            clientFactory.Object,
            cache ?? CreateCache(),
            configuration,
            Mock.Of<ILogger<GoogleMapsService>>());
    }

    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    private static HttpResponseMessage CreateJsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseFactory;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int CallCount { get; private set; }
        public HttpMethod? LastMethod { get; private set; }
        public Uri? LastRequestUri { get; private set; }
        public string? LastBody { get; private set; }
        public Dictionary<string, List<string>> LastHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastMethod = request.Method;
            LastRequestUri = request.RequestUri;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            LastHeaders.Clear();
            foreach (var header in request.Headers)
            {
                LastHeaders[header.Key] = header.Value.ToList();
            }

            return await _responseFactory(request, cancellationToken);
        }
    }
}
