namespace DriverService.API.Models;

/// <summary>
/// Canonical definitions for driver performance metrics.
/// These definitions are reused by later query/endpoint work.
/// </summary>
public static class DriverPerformanceMetricDefinitions
{
    public static IReadOnlyList<DriverPerformanceMetricDefinition> All { get; } =
    [
        new()
        {
            Key = "deliveries_completed",
            DisplayName = "Deliveries Completed",
            Description = "Number of deliveries completed by the driver in the selected period.",
            Unit = "count",
            Formula = "count(deliveries where status = Completed and assigned_driver_id = driverId)"
        },
        new()
        {
            Key = "average_delivery_time_minutes",
            DisplayName = "Average Delivery Time",
            Description = "Average duration from dispatch/start to completion for the driver's deliveries.",
            Unit = "minutes",
            Formula = "avg(completed_at - started_at for driver's completed deliveries)"
        },
        new()
        {
            Key = "on_time_rate_percent",
            DisplayName = "On-Time Rate",
            Description = "Percentage of completed deliveries finished on or before deadline.",
            Unit = "percent",
            Formula = "(on_time_deliveries / completed_deliveries) * 100"
        }
    ];
}

public class DriverPerformanceMetricDefinition
{
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string Unit { get; set; }
    public required string Formula { get; set; }
}
