namespace UserService.API.DTOs;

public record CreateDriverAccountResponse(
    UserResponse User,
    DriverProfileResponse Driver
);

public record DriverProfileResponse(
    int DriverId,
    string UserId,
    string FullName,
    string LicenseNumber,
    string LicenseExpiry,
    string PhoneNumber,
    double MaxWorkingHoursPerDay,
    double CurrentWorkingHoursToday,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
