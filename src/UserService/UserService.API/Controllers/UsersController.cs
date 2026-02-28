using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.API.Services;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // GET /api/users — Admin only
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok("get all users placeholder");
    }

    // GET /api/users/{id} — any authenticated user
    [Authorize]
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        return Ok($"get user {id} placeholder");
    }

    // GET /api/users/me — any authenticated user
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        return Ok("get me placeholder");
    }

    // PUT /api/users/{id} — Admin only
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public IActionResult Update(int id)
    {
        return Ok($"update user {id} placeholder");
    }

    // DELETE /api/users/{id} — Admin only
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public IActionResult SoftDelete(int id)
    {
        return Ok($"delete user {id} placeholder");
    }
}
