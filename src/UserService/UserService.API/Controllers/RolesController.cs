using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.API.DTOs;
using UserService.API.Models;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    /// <summary>
    /// List all predefined roles. Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IEnumerable<RoleDto>> GetRoles()
    {
        var roles = Role.GetAll().Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description
        }).ToList();

        return Ok(roles);
    }

    /// <summary>
    /// Return the role of the currently authenticated user (string) or null if
    /// not authenticated.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<string?> GetMyRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value
                        ?? User.FindFirst("role")?.Value;
        return Ok(roleClaim);
    }
}
