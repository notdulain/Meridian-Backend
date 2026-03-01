namespace DeliveryService.API.Models;

/// <summary>
/// Represents the roles available in the system. Values should align with the
/// `role` claim issued in JWTs by the authentication service.
/// </summary>
public enum UserRole
{
    Admin = 1,
    Dispatcher = 2,
    Driver = 3
}
