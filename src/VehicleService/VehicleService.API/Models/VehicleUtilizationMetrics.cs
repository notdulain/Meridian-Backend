namespace VehicleService.API.Models;

/// <summary>
/// Definition contract for vehicle utilization metrics.
/// This defines metric names and formulas only; report query and endpoint are implemented in later tasks.
/// </summary>
public class VehicleUtilizationMetrics
{
    public int VehicleId { get; set; }
    public int TripsCount { get; set; }
    public double KilometersDriven { get; set; }
    public double IdleTimeMinutes { get; set; }
}

/// <summary>
/// Central definitions for vehicle utilization metrics used by reporting.
/// </summary>
public static class VehicleUtilizationMetricDefinitions
{
    public const string TripsCount =
        "Count of completed trips for a vehicle in the selected window. A completed trip maps to delivery status 'Delivered'.";

    public const string KilometersDriven =
        "Total kilometers driven by a vehicle in the selected window, summed across completed trips.";

    public const string IdleTimeMinutes =
        "Total idle minutes in the selected window. Calculated as reportWindowMinutes - activeTripMinutes.";

    public const string ActiveTripMinutes =
        "Active trip minutes are derived from assigned trips using delivery lifecycle timestamps (CreatedAt to UpdatedAt), clipped to the report window.";
}