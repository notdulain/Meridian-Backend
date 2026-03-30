using DeliveryService.API.Models;

namespace DeliveryService.API.Services;

public interface IDeliveryReportService
{
    Task<DeliverySuccessRateSummary> GetDeliverySuccessRateAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CancellationToken cancellationToken = default);
}