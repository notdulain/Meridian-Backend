namespace ApiGateway.Models;

public class DashboardSummaryDto
{
    public int TotalDeliveries { get; set; }
    public int ActiveDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public int OverdueDeliveries { get; set; }
    public int AvailableVehicles { get; set; }
    public int VehiclesOnTrip { get; set; }
    public int AvailableDrivers { get; set; }
    public int ActiveAssignments { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
