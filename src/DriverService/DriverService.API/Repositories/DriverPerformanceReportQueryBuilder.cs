namespace DriverService.API.Repositories;

/// <summary>
/// Builds SQL queries for driver performance reporting.
/// The delivery aggregation runs against the delivery database connection directly,
/// so the query must not use cross-database names that Azure SQL Database rejects.
/// </summary>
public static class DriverPerformanceReportQueryBuilder
{
    public static string BuildMetricsQuery() =>
        @"
SELECT
    del.AssignedDriverId AS DriverId,
    COUNT(CASE WHEN del.Status = 'Completed' THEN 1 END) AS DeliveriesCompleted,
    COALESCE(AVG(CASE
        WHEN del.Status = 'Completed' THEN DATEDIFF(MINUTE, del.CreatedAt, del.UpdatedAt)
    END), 0) AS AverageDeliveryTimeMinutes,
    CASE
        WHEN COUNT(CASE WHEN del.Status = 'Completed' THEN 1 END) = 0 THEN 0
        ELSE
            100.0 * SUM(CASE
                WHEN del.Status = 'Completed' AND del.UpdatedAt <= del.Deadline THEN 1
                ELSE 0
            END)
            / COUNT(CASE WHEN del.Status = 'Completed' THEN 1 END)
    END AS OnTimeRatePercent
FROM Deliveries del
WHERE del.AssignedDriverId IS NOT NULL
    AND (@StartDateUtc IS NULL OR del.CreatedAt >= @StartDateUtc)
    AND (@EndDateUtc IS NULL OR del.CreatedAt < @EndDateUtc)
GROUP BY del.AssignedDriverId
ORDER BY del.AssignedDriverId;";
}
