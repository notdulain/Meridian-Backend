namespace DeliveryService.API.Models;

public class StatusHistory
{
    public int StatusHistoryId { get; set; }
    public int DeliveryId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public int ChangedBy { get; set; }
    public string? Notes { get; set; }
}
