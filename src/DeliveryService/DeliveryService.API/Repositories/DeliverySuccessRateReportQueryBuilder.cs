namespace DeliveryService.API.Repositories;

/// <summary>
/// Builds SQL for delivery success-rate aggregation.
/// This task only defines the aggregation query; endpoint/report generation is added later.
/// </summary>
public static class DeliverySuccessRateReportQueryBuilder
{
    public static string BuildSuccessRateAggregationQuery()
    {
        return """
SELECT
    SUM(CASE WHEN d.Status = 'Delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
    SUM(CASE WHEN d.Status = 'Failed' THEN 1 ELSE 0 END) AS FailedCount,
    SUM(CASE WHEN d.Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledCount,
    SUM(CASE WHEN d.Status IN ('Delivered', 'Failed', 'Cancelled') THEN 1 ELSE 0 END) AS TerminalCount,
    CASE
        WHEN SUM(CASE WHEN d.Status IN ('Delivered', 'Failed', 'Cancelled') THEN 1 ELSE 0 END) = 0 THEN 0
        ELSE
            100.0 * SUM(CASE WHEN d.Status = 'Delivered' THEN 1 ELSE 0 END)
            / SUM(CASE WHEN d.Status IN ('Delivered', 'Failed', 'Cancelled') THEN 1 ELSE 0 END)
    END AS SuccessRatePercentage
FROM Deliveries d
WHERE (@StartDateUtc IS NULL OR d.CreatedAt >= @StartDateUtc)
  AND (@EndDateUtc IS NULL OR d.CreatedAt < @EndDateUtc);
""";
    }
}