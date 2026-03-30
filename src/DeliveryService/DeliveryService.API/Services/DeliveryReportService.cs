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
}