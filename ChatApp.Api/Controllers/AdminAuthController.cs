using ChatApp.Shared.Models;
using ChatApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, response) = await auth.AdminLoginAsync(request.Username, request.Password);
        return success ? Ok(response) : BadRequest(ApiResult.Fail(error!));
    }
}
