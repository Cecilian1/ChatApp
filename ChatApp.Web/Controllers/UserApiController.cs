using ChatApp.Shared.Models;
using ChatApp.Web.Filters;
using ChatApp.Web.Services.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

[Route("api/User")]
[ApiController]
[RequireLogin]
public class UserApiController(ApiHttpClient api) : ControllerBase
{
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var result = await api.PutAsync<UserDto>("api/user/profile", request);
            return Ok(result);
        }
        catch (ApiException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class UpdateProfileRequest
{
    public string? Nickname { get; set; }
    public string? AvatarSeed { get; set; }
}
