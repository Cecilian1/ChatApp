using ChatApp.Shared.Models;
using ChatApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatApp.Api.Data;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService auth, AppDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var (success, error) = await auth.RegisterAsync(request);
        return success ? Ok(ApiResult.Ok()) : BadRequest(ApiResult.Fail(error!));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, response) = await auth.LoginAsync(request.Username, request.Password);
        return success ? Ok(response) : BadRequest(ApiResult.Fail(error!));
    }

    [Authorize(Roles = "user")]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        return user is null ? NotFound() : Ok(AuthService.MapUser(user));
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
