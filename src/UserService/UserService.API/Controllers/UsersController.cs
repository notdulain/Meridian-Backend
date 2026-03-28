using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.API.DTOs;
using UserService.API.Exceptions;
using UserService.API.Services;
using System.Security.Claims;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IDriverAccountProvisioningService _driverAccountProvisioningService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        IDriverAccountProvisioningService driverAccountProvisioningService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _driverAccountProvisioningService = driverAccountProvisioningService;
        _logger = logger;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("driver-accounts")]
    [ProducesResponseType(typeof(CreateDriverAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDriverAccount([FromBody] CreateDriverAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var authorizationHeader = Request.Headers.Authorization.ToString();
            var result = await _driverAccountProvisioningService.CreateDriverAccountAsync(
                request,
                authorizationHeader,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
        }
        catch (ResourceConflictException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // GET /api/users — Admin only
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var users = (await _userService.GetAllAsync()).ToList();

            if (users.Count == 0)
            {
                return NotFound(new { message = "No users found." });
            }

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve users from UserService.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving users." });
        }
    }

    // GET /api/users/{id} — any authenticated user
    [Authorize]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && currentUserId != id)
        {
            return Forbid();
        }

        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(user);
    }

    // GET /api/users/me — any authenticated user
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var user = await _userService.GetMeAsync(userId);
        if (user is null) return NotFound();
        return Ok(user);
    }

    // PUT /api/users/{id} — Admin only
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateAsync(id, request);
        if (user is null) return NotFound();
        return Ok(user);
    }

    // DELETE /api/users/{id} — Admin only
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var deleted = await _userService.SoftDeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
