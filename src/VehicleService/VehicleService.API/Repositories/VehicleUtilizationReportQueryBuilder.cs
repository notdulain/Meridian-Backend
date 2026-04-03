using System.Text.RegularExpressions;

namespace VehicleService.API.Repositories;

/// <summary>
/// Builds SQL for vehicle utilization reporting metrics.
/// This task defines the query only; endpoint wiring is implemented later.
/// </summary>
public static class VehicleUtilizationReportQueryBuilder
{
    private static readonly Regex ValidDbName = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public static string BuildMetricsQuery(string deliveryDatabaseName, string routeDatabaseName)
    {
        if (string.IsNullOrWhiteSpace(deliveryDatabaseName))
            throw new ArgumentException("Delivery database name is required.", nameof(deliveryDatabaseName));

        if (!ValidDbName.IsMatch(deliveryDatabaseName))
            throw new ArgumentException("Delivery database name contains invalid characters.", nameof(deliveryDatabaseName));

        if (string.IsNullOrWhiteSpace(routeDatabaseName))
            throw new ArgumentException("Route database name is required.", nameof(routeDatabaseName));

        if (!ValidDbName.IsMatch(routeDatabaseName))
            throw new ArgumentException("Route database name contains invalid characters.", nameof(routeDatabaseName));

        return $@"
WITH WindowBounds AS (
    SELECT
        COALESCE(@StartDateUtc, DATEADD(DAY, -30, SYSUTCDATETIME())) AS WindowStartUtc,
        COALESCE(@EndDateUtc, SYSUTCDATETIME()) AS WindowEndUtc
),
DeliveredTrips AS (
    SELECT
        d.AssignedVehicleId AS VehicleId,
        d.DeliveryId,
        d.PickupAddress,
        d.DeliveryAddress,
        CASE
            WHEN d.UpdatedAt > d.CreatedAt THEN DATEDIFF(MINUTE, d.CreatedAt, d.UpdatedAt)
            ELSE 0
        END AS TripMinutes
    FROM [{deliveryDatabaseName}].[dbo].[Deliveries] d
    CROSS JOIN WindowBounds wb
    WHERE d.AssignedVehicleId IS NOT NULL
      AND d.Status = 'Delivered'
      AND d.UpdatedAt >= wb.WindowStartUtc
      AND d.UpdatedAt < wb.WindowEndUtc
),
TripsWithDistance AS (
    SELECT
        dt.VehicleId,
        dt.DeliveryId,
        dt.TripMinutes,
        COALESCE(routeMatch.DistanceKm, 0) AS DistanceKm
    FROM DeliveredTrips dt
    OUTER APPLY (
        SELECT TOP 1 rh.DistanceKm
        FROM [{routeDatabaseName}].[dbo].[RouteHistories] rh
        WHERE rh.Selected = 1
          AND rh.Origin = dt.PickupAddress
          AND rh.Destination = dt.DeliveryAddress
        ORDER BY rh.CreatedAt DESC
    ) routeMatch
),
Aggregated AS (
    SELECT
        twd.VehicleId,
        COUNT(*) AS TripsCount,
        COALESCE(SUM(twd.DistanceKm), 0) AS KilometersDriven,
        COALESCE(SUM(twd.TripMinutes), 0) AS ActiveTripMinutes
    FROM TripsWithDistance twd
    GROUP BY twd.VehicleId
)
SELECT
    v.VehicleId,
    COALESCE(a.TripsCount, 0) AS TripsCount,
    COALESCE(a.KilometersDriven, 0) AS KilometersDriven,
    CASE
        WHEN DATEDIFF(MINUTE, wb.WindowStartUtc, wb.WindowEndUtc) <= COALESCE(a.ActiveTripMinutes, 0) THEN 0
        ELSE DATEDIFF(MINUTE, wb.WindowStartUtc, wb.WindowEndUtc) - COALESCE(a.ActiveTripMinutes, 0)
    END AS IdleTimeMinutes
FROM Vehicles v
CROSS JOIN WindowBounds wb
LEFT JOIN Aggregated a
    ON a.VehicleId = v.VehicleId
WHERE v.Status <> 'Retired'
ORDER BY v.VehicleId;";
    }
}