namespace TrackingService.API.Models;

public class LocationUpdate
{
    public int LocationUpdateId { get; set; }
    public int AssignmentId { get; set; }
    public int DriverId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal? SpeedKmh { get; set; }
}
