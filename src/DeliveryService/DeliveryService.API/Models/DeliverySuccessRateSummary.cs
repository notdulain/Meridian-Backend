namespace DeliveryService.API.Models;

public class DeliverySuccessRateSummary
{
    public int DeliveredCount { get; init; }
    public int FailedCount { get; init; }
    public int CancelledCount { get; init; }
    public int TerminalCount { get; init; }
    public decimal SuccessRatePercentage { get; init; }
}