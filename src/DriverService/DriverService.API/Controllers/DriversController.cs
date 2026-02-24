using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DriverService.API.Models;

namespace DriverService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriversController : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult CreateDriver([FromBody] Driver driver)
    {
        // Placeholder implementation
        driver.DriverId = 1;
        return StatusCode(201, new { success = true, data = driver });
    }

    [HttpGet]
    public IActionResult GetDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // Placeholder implementation
        var drivers = new List<Driver>();
        return Ok(new { success = true, data = drivers, meta = new { page, pageSize, totalCount = 0 } });
    }

    [HttpGet("{id}")]
    public IActionResult GetDriver(int id)
    {
        // Placeholder implementation
        return Ok(new { success = true, data = new Driver { DriverId = id, KeycloakUserId = "sub", FullName = "John Doe", LicenseNumber = "XYZ", LicenseExpiry = "2025", PhoneNumber = "xxx" } });
    }

    [HttpGet("me")]
    [Authorize(Roles = "Driver")]
    public IActionResult GetMyProfile()
    {
        // Placeholder implementation - read sub claim
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Ok(new { success = true, data = new Driver { DriverId = 1, KeycloakUserId = sub ?? "sub", FullName = "My Name", LicenseNumber = "XYZ", LicenseExpiry = "2025", PhoneNumber = "xxx" } });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public IActionResult UpdateDriver(int id, [FromBody] Driver driver)
    {
        // Placeholder implementation
        driver.DriverId = id;
        return Ok(new { success = true, data = driver });
    }

    [HttpPut("{id}/hours")]
    public IActionResult UpdateWorkingHours(int id, [FromBody] UpdateHoursDto request)
    {
        // Placeholder implementation
        return Ok(new { success = true, message = "Hours updated" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteDriver(int id)
    {
        // Placeholder implementation (soft delete)
        return Ok(new { success = true, message = "Driver deleted" });
    }

    [HttpGet("available")]
    public IActionResult GetAvailableDrivers()
    {
        // Placeholder implementation
        var drivers = new List<Driver>();
        return Ok(new { success = true, data = drivers });
    }
}

public class UpdateHoursDto
{
    public double HoursToAdd { get; set; }
}
