using Microsoft.AspNetCore.Mvc;
using RouteService.API.Models;
using RouteService.API.Services;

namespace RouteService.API.Controllers;

[ApiController]

[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly IGoogleMapsService _googleMapsService;

    public RoutesController(IGoogleMapsService googleMapsService)
    {
        _googleMapsService = googleMapsService;
    }

    [HttpPost("optimize")]
    public async Task<IActionResult> OptimizeRoute([FromBody] OptimizeRouteRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Origin) || string.IsNullOrWhiteSpace(request.Destination))
        {
            return BadRequest(new { success = false, message = "Both origin and destination are required." });
        }

        try
        {
            var routes = await _googleMapsService.GetAlternativeRoutesAsync(request.Origin, request.Destination, cancellationToken);

            if (routes.Count == 0)
            {
                return NotFound(new { success = false, message = "No routes found for the provided origin and destination." });
            }

            var optimizedRoute = routes[0];
            var alternatives = routes.Skip(1).ToList();

            return Ok(new
            {
                success = true,
                optimizedRoute,
                alternatives
            });
        }
        catch (RouteNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (GoogleMapsServiceException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = ex.Message });
        }
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

    [HttpGet("alternatives")]
    public async Task<IActionResult> GetAlternativeRoutes([FromQuery] string origin, [FromQuery] string destination, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest(new { success = false, message = "Both origin and destination are required." });
        }

        try
        {
            var routes = await _googleMapsService.GetAlternativeRoutesAsync(origin, destination, cancellationToken);
            return Ok(new { success = true, routes });
        }
        catch (RouteNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (GoogleMapsServiceException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = ex.Message });
        }
    }

    /// <summary>GET /api/routes/compare?origin=Colombo&amp;destination=Kandy - Compare alternative routes with recommended route by fuel cost, distance, and ETA (MER-67).</summary>
    [HttpGet("compare")]
    public async Task<IActionResult> CompareRoutes([FromQuery] string origin, [FromQuery] string destination, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest(new { success = false, message = "Both origin and destination are required." });
        }

        try
        {
            var routes = await _googleMapsService.GetRouteComparisonsAsync(origin, destination, cancellationToken);
            var recommended = routes.FirstOrDefault(r => r.IsRecommended);
            var response = new RouteComparisonResponse
            {
                Success = true,
                Routes = routes,
                RecommendedRouteId = recommended?.RouteId ?? string.Empty
            };
            return Ok(response);
        }
        catch (RouteNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (GoogleMapsServiceException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = ex.Message });
        }
    }

    /// <summary>GET /api/routes/rank?origin=Colombo&amp;destination=Kandy - Rank route options by fuel cost, distance, and duration (fuel in litres, cost in LKR, duration in hours).</summary>
    [HttpGet("rank")]
    public async Task<IActionResult> GetRankedRoutes([FromQuery] string origin, [FromQuery] string destination, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest(new { success = false, message = "Both origin and destination are required." });
        }

        try
        {
            var response = await _googleMapsService.GetRankedRoutesAsync(origin, destination, cancellationToken);
            return Ok(response);
        }
        catch (RouteNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (GoogleMapsServiceException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = ex.Message });
        }
    }
}

