using System;

namespace DeliveryService.API.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Predefined role constants
    // Predefined role constants Admin, Dispatcher, Driver
    public static readonly Role Admin = new() { Id = 1, Name = "Admin", Description = "Full system access and user management" };
    public static readonly Role Dispatcher = new() { Id = 2, Name = "Dispatcher", Description = "Manage deliveries and assign drivers" };
    public static readonly Role Driver = new() { Id = 3, Name = "Driver", Description = "View assigned deliveries and update status" };

    /// <summary>
    /// Returns all available roles.
    /// </summary>
    public static List<Role> GetAll() => [Admin, Dispatcher, Driver];
}
