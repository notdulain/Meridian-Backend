using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DriverService.API.Models;
using DriverService.API.Services;

namespace DriverService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriversController : ControllerBase
{
    private readonly IDriverService _service;

    public DriversController(IDriverService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDriver([FromBody] Driver driver)
    {
        try
        {
            var created = await _service.CreateDriverAsync(driver);
            return StatusCode(201, new { success = true, data = created });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to create driver", errors = new[] { ex.Message } });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var (drivers, totalCount) = await _service.GetDriversAsync(page, pageSize);
            return Ok(new { success = true, data = drivers, meta = new { page, pageSize, totalCount } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch drivers", errors = new[] { ex.Message } });
        }
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeletedDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var (drivers, totalCount) = await _service.GetDeletedDriversAsync(page, pageSize);
            return Ok(new { success = true, data = drivers, meta = new { page, pageSize, totalCount } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch deleted drivers", errors = new[] { ex.Message } });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetDriver(int id)
    {
        try
        {
            var driver = await _service.GetDriverByIdAsync(id);
            if (driver == null) return NotFound(new { success = false, message = "Driver not found", errors = Array.Empty<string>() });

            return Ok(new { success = true, data = driver });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch driver", errors = new[] { ex.Message } });
        }
    }

    [HttpGet("me")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(sub))
                return Unauthorized(new { success = false, message = "Missing user identity.", errors = Array.Empty<string>() });

            var driver = await _service.GetDriverByUserIdAsync(sub);
            if (driver == null)
                return NotFound(new { success = false, message = "Driver profile not found.", errors = Array.Empty<string>() });

            return Ok(new { success = true, data = driver });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch current driver profile", errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDriver(int id, [FromBody] Driver driver)
    {
        try
        {
            var existing = await _service.GetDriverByIdAsync(id);
            if (existing == null) return NotFound(new { success = false, message = "Driver not found", errors = Array.Empty<string>() });

            var updated = await _service.UpdateDriverAsync(id, driver);
            return Ok(new { success = true, data = updated });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to update driver", errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id}/hours")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> UpdateWorkingHours(int id, [FromBody] UpdateHoursDto request)
    {
        try
        {
            var success = await _service.UpdateWorkingHoursAsync(id, request.HoursToAdd);
            if (!success) return NotFound(new { success = false, message = "Driver not found", errors = Array.Empty<string>() });

            return Ok(new { success = true, message = "Working hours updated" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to update working hours", errors = new[] { ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDriver(int id)
    {
        try
        {
            var success = await _service.DeleteDriverAsync(id);
            if (!success) return NotFound(new { success = false, message = "Driver not found", errors = Array.Empty<string>() });

            return Ok(new { success = true, message = "Driver deleted successfully (soft delete)" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to delete driver", errors = new[] { ex.Message } });
        }
    }

    [HttpGet("available")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetAvailableDrivers()
    {
        try
        {
            var drivers = await _service.GetAvailableDriversAsync();
            return Ok(new { success = true, data = drivers });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch available drivers", errors = new[] { ex.Message } });
        }
    }
}

public class UpdateHoursDto
{
    public double HoursToAdd { get; set; }
}
