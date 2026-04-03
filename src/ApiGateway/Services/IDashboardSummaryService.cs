using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IDashboardSummaryService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
