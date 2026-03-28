namespace UserService.API.DTOs;

public class CreateDriverAccountRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseExpiry { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public double MaxWorkingHoursPerDay { get; set; } = 8.0;
}
