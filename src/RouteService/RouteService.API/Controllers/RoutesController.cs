using Microsoft.AspNetCore.Mvc;
using RouteService.API.Models;
using RouteService.API.Services;

namespace RouteService.API.Controllers;

[ApiController]

[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly IGoogleMapsService _googleMapsService;
    private readonly IRouteDecisionService _routeDecisionService;

    public RoutesController(
        IGoogleMapsService googleMapsService,
        IRouteDecisionService routeDecisionService)
    {
        _googleMapsService = googleMapsService;
        _routeDecisionService = routeDecisionService;
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

    [HttpPost("select")]
    public async Task<IActionResult> SelectRoute([FromBody] SelectRouteRequest request, CancellationToken cancellationToken)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.Origin)
            || string.IsNullOrWhiteSpace(request.Destination)
            || request.Route is null)
        {
            return BadRequest(new { success = false, message = "Origin, destination, and route are required." });
        }

        try
        {
            var selected = await _routeDecisionService.SaveSelectedRouteAsync(request, cancellationToken);
            return Ok(new { success = true, route = selected });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetRouteHistory([FromQuery] string origin, [FromQuery] string destination, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest(new { success = false, message = "Both origin and destination are required." });
        }

        var history = await _routeDecisionService.GetHistoryAsync(origin, destination, cancellationToken);
        return Ok(new { success = true, routes = history });
    }

    [HttpGet("compare")]
    public async Task<IActionResult> CompareRoutes([FromQuery] string origin, [FromQuery] string destination, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest(new { success = false, message = "Both origin and destination are required." });
        }

        var response = await _routeDecisionService.CompareRoutesAsync(origin, destination, cancellationToken);
        return Ok(response);
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

