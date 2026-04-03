using DeliveryService.API.Models;
using DeliveryService.API.Repositories;

namespace DeliveryService.API.Services;

public class DeliveryReportService : IDeliveryReportService
{
    private readonly DeliveryRepository _repository;

    public DeliveryReportService(DeliveryRepository repository)
    {
        _repository = repository;
    }

    public Task<DeliverySuccessRateSummary> GetDeliverySuccessRateAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CancellationToken cancellationToken = default)
    {
        if (startDateUtc.HasValue && endDateUtc.HasValue && startDateUtc.Value >= endDateUtc.Value)
        {
            throw new ArgumentException("startDateUtc must be earlier than endDateUtc.");
        }

        return _repository.GetDeliverySuccessRateSummaryAsync(startDateUtc, endDateUtc, cancellationToken);
    }

    public Task<IEnumerable<DeliveryTrendPoint>> GetDeliveryTrendsAsync(
        string range,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        // Apply sensible defaults when the caller omits date bounds
        var resolvedFrom = from ?? range switch
        {
            "weekly"  => DateTime.UtcNow.AddDays(-84),   // last 12 weeks
            "monthly" => DateTime.UtcNow.AddMonths(-12),  // last 12 months
            _         => DateTime.UtcNow.AddDays(-30),    // last 30 days (daily)
        };

        var resolvedTo = to ?? DateTime.UtcNow.AddDays(1); // include today

        if (resolvedFrom >= resolvedTo)
            throw new ArgumentException("'from' must be earlier than 'to'.");

        return _repository.GetDeliveryTrendsAsync(range, resolvedFrom, resolvedTo, cancellationToken);
    }
}