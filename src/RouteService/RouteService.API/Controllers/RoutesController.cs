using Microsoft.AspNetCore.Mvc;
using RouteService.API.Models;
using RouteService.API.Services;

namespace RouteService.API.Controllers;

[ApiController]
[Route("routes")]
public class RoutesController : ControllerBase
{
    private readonly IGoogleMapsService _googleMapsService;

    public RoutesController(IGoogleMapsService googleMapsService)
    {
        _googleMapsService = googleMapsService;
    }

    [HttpPost("optimize")]
    public IActionResult OptimizeRoute([FromBody] OptimizeRouteRequest request)
    {
        // Placeholder implementation
        var options = new List<RouteOption>
        {
            new RouteOption { RouteId = "R1", Summary = "Fastest", Distance = "10 km", Duration = "15 mins", FuelCost = 5.5, PolylinePoints = "" }
        };
        return Ok(new { success = true, data = options });
    }

    [HttpGet("{routeId}")]
    public IActionResult GetRoute(string routeId)
    {
        // Placeholder implementation
        return Ok(new { success = true, data = new RouteOption { RouteId = routeId, Summary = "Fastest", Distance = "10 km", Duration = "15 mins", FuelCost = 5.5, PolylinePoints = "" } });
    }

    [HttpPost("fuel-cost")]
    public IActionResult CalculateFuelCost()
    {
        // Placeholder implementation
        return Ok(new { success = true, fuelCost = 10.5 });
    }

    [HttpGet("calculate")]
    public async Task<IActionResult> CalculateRoute([FromQuery] string origin, [FromQuery] string destination, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest(new { message = "Both origin and destination are required." });
        }

        try
        {
            var route = await _googleMapsService.GetRouteAsync(origin, destination, cancellationToken);

            return Ok(new
            {
                distance = route.Distance,
                duration = route.Duration,
                polyline = route.Polyline
            });
        }
        catch (RouteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (GoogleMapsServiceException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
