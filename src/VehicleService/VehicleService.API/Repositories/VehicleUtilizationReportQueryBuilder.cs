namespace VehicleService.API.Repositories;

/// <summary>
/// Builds Azure SQL-safe queries for vehicle utilization reporting.
/// </summary>
public static class VehicleUtilizationReportQueryBuilder
{
    public static string BuildDeliveredTripsQuery() =>
        @"
SELECT
    d.AssignedVehicleId AS VehicleId,
    d.PickupAddress,
    d.DeliveryAddress,
    CASE
        WHEN d.UpdatedAt > d.CreatedAt THEN DATEDIFF(MINUTE, d.CreatedAt, d.UpdatedAt)
        ELSE 0
    END AS TripMinutes
FROM Deliveries d
WHERE d.AssignedVehicleId IS NOT NULL
  AND d.Status = 'Delivered'
  AND (@StartDateUtc IS NULL OR d.UpdatedAt >= @StartDateUtc)
  AND (@EndDateUtc IS NULL OR d.UpdatedAt < @EndDateUtc);";

    public static string BuildSelectedRouteHistoriesQuery() =>
        @"
SELECT
    Origin,
    Destination,
    DistanceKm,
    CreatedAt
FROM RouteHistories
WHERE Selected = 1;";
}
