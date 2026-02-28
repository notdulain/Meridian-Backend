using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.API.Services;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register — public
    [HttpPost("register")]
    public IActionResult Register()
    {
        return Ok("register placeholder");
    }

    // POST /api/auth/login — public
    [HttpPost("login")]
    public IActionResult Login()
    {
        return Ok("login placeholder");
    }

    // POST /api/auth/refresh — public
    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        return Ok("refresh placeholder");
    }

    // POST /api/auth/revoke — requires auth
    [Authorize]
    [HttpPost("revoke")]
    public IActionResult Revoke()
    {
        return Ok("revoke placeholder");
    }
}
