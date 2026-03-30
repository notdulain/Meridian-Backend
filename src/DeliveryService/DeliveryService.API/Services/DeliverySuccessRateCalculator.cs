using DeliveryService.API.Models;

namespace DeliveryService.API.Services;

public static class DeliverySuccessRateCalculator
{
    public static DeliverySuccessRateSummary Calculate(int deliveredCount, int failedCount, int cancelledCount)
    {
        if (deliveredCount < 0) throw new ArgumentOutOfRangeException(nameof(deliveredCount));
        if (failedCount < 0) throw new ArgumentOutOfRangeException(nameof(failedCount));
        if (cancelledCount < 0) throw new ArgumentOutOfRangeException(nameof(cancelledCount));

        var terminalCount = deliveredCount + failedCount + cancelledCount;
        var successRatePercentage = terminalCount == 0
            ? 0m
            : Math.Round((deliveredCount * 100m) / terminalCount, 2, MidpointRounding.AwayFromZero);

        return new DeliverySuccessRateSummary
        {
            DeliveredCount = deliveredCount,
            FailedCount = failedCount,
            CancelledCount = cancelledCount,
            TerminalCount = terminalCount,
            SuccessRatePercentage = successRatePercentage
        };
    }

    public static DeliverySuccessRateSummary CalculateFromDeliveries(IEnumerable<Delivery> deliveries)
    {
        ArgumentNullException.ThrowIfNull(deliveries);

        var deliveredCount = 0;
        var failedCount = 0;
        var cancelledCount = 0;

        foreach (var delivery in deliveries)
        {
            var status = delivery.Status?.Trim();

            if (string.Equals(status, "Delivered", StringComparison.OrdinalIgnoreCase))
            {
                deliveredCount++;
            }
            else if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                failedCount++;
            }
            else if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                cancelledCount++;
            }
        }

        return Calculate(deliveredCount, failedCount, cancelledCount);
    }
}