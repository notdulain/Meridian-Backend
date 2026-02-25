using Microsoft.AspNetCore.Mvc;
using VehicleService.API.Models;
using VehicleService.API.Services;

namespace VehicleService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _service;

    public VehiclesController(IVehicleService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> CreateVehicle([FromBody] Vehicle vehicle)
    {
        try
        {
            var created = await _service.CreateVehicleAsync(vehicle);
            return StatusCode(201, new { success = true, data = created });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to create vehicle", errors = new[] { ex.Message } });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicles([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        try
        {
            var (vehicles, totalCount) = await _service.GetVehiclesAsync(page, pageSize, status);
            return Ok(new
            {
                success = true,
                data = vehicles,
                meta = new { page, pageSize, totalCount }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch vehicles", errors = new[] { ex.Message } });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicle(int id)
    {
        try
        {
            var vehicle = await _service.GetVehicleByIdAsync(id);
            if (vehicle == null) return NotFound(new { success = false, message = "Vehicle not found", errors = Array.Empty<string>() });

            return Ok(new { success = true, data = vehicle });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch vehicle", errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] Vehicle vehicle)
    {
        try
        {
            var existing = await _service.GetVehicleByIdAsync(id);
            if (existing == null) return NotFound(new { success = false, message = "Vehicle not found", errors = Array.Empty<string>() });

            var updated = await _service.UpdateVehicleAsync(id, vehicle);
            return Ok(new { success = true, data = updated });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to update vehicle", errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto request)
    {
        try
        {
            var success = await _service.UpdateVehicleStatusAsync(id, request.Status);
            if (!success) return NotFound(new { success = false, message = "Vehicle not found or status not updated", errors = Array.Empty<string>() });

            return Ok(new { success = true, message = "Status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to update status", errors = new[] { ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        try
        {
            var success = await _service.DeleteVehicleAsync(id);
            if (!success) return NotFound(new { success = false, message = "Vehicle not found", errors = Array.Empty<string>() });

            return Ok(new { success = true, message = "Vehicle deleted successfully (soft delete)" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to delete vehicle", errors = new[] { ex.Message } });
        }
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableVehicles()
    {
        try
        {
            var vehicles = await _service.GetAvailableVehiclesAsync();
            return Ok(new { success = true, data = vehicles });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch available vehicles", errors = new[] { ex.Message } });
        }
    }
}

public class UpdateStatusRequestDto
{
    public required string Status { get; set; }
}
