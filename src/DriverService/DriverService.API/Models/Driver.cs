namespace DriverService.API.Models;

public class Driver
{
    public int DriverId { get; set; }
    public required string KeycloakUserId { get; set; }
    public required string FullName { get; set; }
    public required string LicenseNumber { get; set; }
    public required string LicenseExpiry { get; set; }
    public required string PhoneNumber { get; set; }
    public double MaxWorkingHoursPerDay { get; set; }
    public double CurrentWorkingHoursToday { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
