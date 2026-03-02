namespace AssignmentService.API.Models;

public class Assignment
{
    public int AssignmentId { get; set; }
    public int DeliveryId { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public DateTime AssignedAt { get; set; }
    public required string AssignedBy { get; set; }
    public required string Status { get; set; } // Active, Completed, Cancelled
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
