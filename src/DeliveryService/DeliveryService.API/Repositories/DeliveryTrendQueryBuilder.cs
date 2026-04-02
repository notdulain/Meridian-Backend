namespace DeliveryService.API.Repositories;

/// <summary>
/// Builds the raw SQL for delivery trend aggregation queries.
/// Produces one row per time bucket (day / week / month) with
/// counts broken down by delivery status.
/// </summary>
public static class DeliveryTrendQueryBuilder
{
    /// <summary>
    /// Returns a SQL query that groups deliveries by the requested period granularity.
    /// Supported values: "daily", "weekly", "monthly".
    /// </summary>
    public static string Build(string range) => range.ToLowerInvariant() switch
    {
        "weekly"  => BuildWeekly(),
        "monthly" => BuildMonthly(),
        _         => BuildDaily(),   // default = daily
    };

    // ── Daily ──────────────────────────────────────────────────────────────────
    // Groups by calendar date. Period label: "2026-03-01"
    private static string BuildDaily() => """
        SELECT
            CONVERT(VARCHAR(10), CreatedAt, 120)  AS Period,
            COUNT(*)                               AS Total,
            SUM(CASE WHEN Status = 'Pending'   THEN 1 ELSE 0 END) AS Pending,
            SUM(CASE WHEN Status = 'Assigned'  THEN 1 ELSE 0 END) AS Assigned,
            SUM(CASE WHEN Status = 'InTransit' THEN 1 ELSE 0 END) AS InTransit,
            SUM(CASE WHEN Status = 'Delivered' THEN 1 ELSE 0 END) AS Delivered,
            SUM(CASE WHEN Status = 'Failed'    THEN 1 ELSE 0 END) AS Failed,
            SUM(CASE WHEN Status = 'Canceled'  THEN 1 ELSE 0 END) AS Canceled
        FROM Deliveries
        WHERE (@From IS NULL OR CreatedAt >= @From)
          AND (@To   IS NULL OR CreatedAt <  @To)
        GROUP BY CONVERT(VARCHAR(10), CreatedAt, 120)
        ORDER BY Period ASC;
        """;

    // ── Weekly ─────────────────────────────────────────────────────────────────
    // Groups by ISO year + week number. Period label: "2026-W12"
    private static string BuildWeekly() => """
        SELECT
            CAST(YEAR(CreatedAt) AS VARCHAR) + '-W'
                + RIGHT('0' + CAST(DATEPART(ISO_WEEK, CreatedAt) AS VARCHAR), 2) AS Period,
            COUNT(*)                               AS Total,
            SUM(CASE WHEN Status = 'Pending'   THEN 1 ELSE 0 END) AS Pending,
            SUM(CASE WHEN Status = 'Assigned'  THEN 1 ELSE 0 END) AS Assigned,
            SUM(CASE WHEN Status = 'InTransit' THEN 1 ELSE 0 END) AS InTransit,
            SUM(CASE WHEN Status = 'Delivered' THEN 1 ELSE 0 END) AS Delivered,
            SUM(CASE WHEN Status = 'Failed'    THEN 1 ELSE 0 END) AS Failed,
            SUM(CASE WHEN Status = 'Canceled'  THEN 1 ELSE 0 END) AS Canceled
        FROM Deliveries
        WHERE (@From IS NULL OR CreatedAt >= @From)
          AND (@To   IS NULL OR CreatedAt <  @To)
        GROUP BY YEAR(CreatedAt), DATEPART(ISO_WEEK, CreatedAt),
                 CAST(YEAR(CreatedAt) AS VARCHAR) + '-W'
                     + RIGHT('0' + CAST(DATEPART(ISO_WEEK, CreatedAt) AS VARCHAR), 2)
        ORDER BY YEAR(CreatedAt), DATEPART(ISO_WEEK, CreatedAt);
        """;

    // ── Monthly ────────────────────────────────────────────────────────────────
    // Groups by year + month. Period label: "2026-03"
    private static string BuildMonthly() => """
        SELECT
            CAST(YEAR(CreatedAt) AS VARCHAR) + '-'
                + RIGHT('0' + CAST(MONTH(CreatedAt) AS VARCHAR), 2) AS Period,
            COUNT(*)                               AS Total,
            SUM(CASE WHEN Status = 'Pending'   THEN 1 ELSE 0 END) AS Pending,
            SUM(CASE WHEN Status = 'Assigned'  THEN 1 ELSE 0 END) AS Assigned,
            SUM(CASE WHEN Status = 'InTransit' THEN 1 ELSE 0 END) AS InTransit,
            SUM(CASE WHEN Status = 'Delivered' THEN 1 ELSE 0 END) AS Delivered,
            SUM(CASE WHEN Status = 'Failed'    THEN 1 ELSE 0 END) AS Failed,
            SUM(CASE WHEN Status = 'Canceled'  THEN 1 ELSE 0 END) AS Canceled
        FROM Deliveries
        WHERE (@From IS NULL OR CreatedAt >= @From)
          AND (@To   IS NULL OR CreatedAt <  @To)
        GROUP BY YEAR(CreatedAt), MONTH(CreatedAt),
                 CAST(YEAR(CreatedAt) AS VARCHAR) + '-'
                     + RIGHT('0' + CAST(MONTH(CreatedAt) AS VARCHAR), 2)
        ORDER BY YEAR(CreatedAt), MONTH(CreatedAt);
        """;
}
