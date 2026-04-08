namespace DriverService.API.Models;

/// <summary>
/// Output shape for a single driver's performance metrics.
/// This is a definition model only; report generation is implemented later.
/// </summary>
public class DriverPerformanceMetrics
{
    public int DriverId { get; set; }
    public int DeliveriesCompleted { get; set; }
    public double AverageDeliveryTimeMinutes { get; set; }
    public double OnTimeRatePercent { get; set; }
}
