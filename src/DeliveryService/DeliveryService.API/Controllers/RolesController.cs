using Microsoft.AspNetCore.Mvc;
using DeliveryService.API.DTOs;
using DeliveryService.API.Models;

namespace DeliveryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    /// <summary>
    /// Retrieve all available user roles.
    /// </summary>
    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
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
    /// Retrieve the role of the current user (if authenticated).
    /// </summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public ActionResult<string?> GetMyRole()
    {
        if (HttpContext.Items.TryGetValue("UserRole", out var obj) && obj is UserRole ur)
        {
            return Ok(ur.ToString());
        }

        // role not available (anon or token missing/invalid)
        return Ok((string?)null);
    }
}
