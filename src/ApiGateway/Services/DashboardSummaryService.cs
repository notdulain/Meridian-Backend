using System.Net.Http.Headers;
using System.Text.Json;
using ApiGateway.Models;

namespace ApiGateway.Services;

public class DashboardSummaryService : IDashboardSummaryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> ActiveDeliveryStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Assigned",
        "InTransit"
    };
    private static readonly HashSet<string> CompletedDeliveryStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Completed",
        "Delivered"
    };
    private static readonly HashSet<string> TerminalDeliveryStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Completed",
        "Delivered",
        "Failed",
        "Cancelled",
        "Canceled"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DashboardSummaryService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var deliveryTask = GetDeliveriesAsync(cancellationToken);
        var vehicleTask = GetPagedItemsAsync<VehicleListItem>("VehicleService", "/api/vehicles", cancellationToken);
        var driverTask = GetPagedItemsAsync<DriverListItem>("DriverService", "/api/drivers", cancellationToken);
        var assignmentTask = GetPagedItemsAsync<AssignmentListItem>("AssignmentService", "/api/assignments", cancellationToken);

        await Task.WhenAll(deliveryTask, vehicleTask, driverTask, assignmentTask);

        var now = DateTime.UtcNow;
        var deliveries = deliveryTask.Result;
        var vehicles = vehicleTask.Result;
        var drivers = driverTask.Result;
        var assignments = assignmentTask.Result;

        return new DashboardSummaryDto
        {
            TotalDeliveries = deliveries.Count,
            ActiveDeliveries = deliveries.Count(d => ActiveDeliveryStatuses.Contains(d.Status)),
            CompletedDeliveries = deliveries.Count(d => CompletedDeliveryStatuses.Contains(d.Status)),
            OverdueDeliveries = deliveries.Count(d => d.Deadline < now && !TerminalDeliveryStatuses.Contains(d.Status)),
            AvailableVehicles = vehicles.Count(v => string.Equals(v.Status, "Available", StringComparison.OrdinalIgnoreCase)),
            VehiclesOnTrip = vehicles.Count(v => string.Equals(v.Status, "OnTrip", StringComparison.OrdinalIgnoreCase)),
            AvailableDrivers = drivers.Count(d => d.IsActive && d.CurrentWorkingHoursToday < d.MaxWorkingHoursPerDay),
            ActiveAssignments = assignments.Count(a => string.Equals(a.Status, "Active", StringComparison.OrdinalIgnoreCase)),
            GeneratedAtUtc = now
        };
    }

    private async Task<List<DeliveryListItem>> GetDeliveriesAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("DeliveryService");
        var results = new List<DeliveryListItem>();
        var page = 1;
        const int pageSize = 500;

        while (true)
        {
            using var request = CreateRequest(HttpMethod.Get, $"/api/deliveries?page={page}&pageSize={pageSize}");
            using var response = await client.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<List<DeliveryListItem>>(contentStream, JsonOptions, cancellationToken) ?? [];
            results.AddRange(payload);

            if (payload.Count < pageSize)
            {
                break;
            }

            page++;
        }

        return results;
    }

    private async Task<List<TItem>> GetPagedItemsAsync<TItem>(string clientName, string path, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(clientName);
        var results = new List<TItem>();
        var page = 1;
        const int pageSize = 250;

        while (true)
        {
            using var request = CreateRequest(HttpMethod.Get, $"{path}?page={page}&pageSize={pageSize}");
            using var response = await client.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<DownstreamPagedEnvelope<List<TItem>>>(contentStream, JsonOptions, cancellationToken);
            var items = payload?.Data ?? [];

            results.AddRange(items);

            var totalCount = payload?.Meta?.TotalCount ?? results.Count;
            if (results.Count >= totalCount || items.Count == 0)
            {
                break;
            }

            page++;
        }

        return results;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authorizationHeader) &&
            AuthenticationHeaderValue.TryParse(authorizationHeader, out var parsedHeader))
        {
            request.Headers.Authorization = parsedHeader;
        }

        return request;
    }
}
