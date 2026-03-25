namespace AssignmentService.API.Models;

public class AssignmentHistory
{
    public int AssignmentHistoryId { get; set; }
    public int AssignmentId { get; set; }
    public int DeliveryId { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public string? PreviousStatus { get; set; }
    public required string NewStatus { get; set; }
    public required string Action { get; set; }
    public required string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
}
