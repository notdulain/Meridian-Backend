using System.Text.RegularExpressions;

namespace DriverService.API.Repositories;

/// <summary>
/// Builds SQL queries for driver performance reporting.
/// This task only defines the join query; endpoint/report generation is added later.
/// </summary>
public static class DriverPerformanceReportQueryBuilder
{
    private static readonly Regex ValidDbName = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public static string BuildMetricsQuery(string deliveryDatabaseName)
    {
        if (string.IsNullOrWhiteSpace(deliveryDatabaseName))
            throw new ArgumentException("Delivery database name is required.", nameof(deliveryDatabaseName));

        if (!ValidDbName.IsMatch(deliveryDatabaseName))
            throw new ArgumentException("Delivery database name contains invalid characters.", nameof(deliveryDatabaseName));

        return $@"
SELECT
    d.DriverId,
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
FROM Drivers d
LEFT JOIN [{deliveryDatabaseName}].[dbo].[Deliveries] del
    ON del.AssignedDriverId = d.DriverId
    AND (@StartDateUtc IS NULL OR del.CreatedAt >= @StartDateUtc)
    AND (@EndDateUtc IS NULL OR del.CreatedAt < @EndDateUtc)
WHERE d.IsActive = 1
GROUP BY d.DriverId
ORDER BY d.DriverId;";
    }
}
