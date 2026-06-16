using ChatApp.Shared.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvatarController : ControllerBase
{
    [HttpGet("svg")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult GetSvg([FromQuery] string? seed, [FromQuery] bool group = false)
    {
        var svg = AvatarGenerator.GenerateSvg(seed, group);
        return Content(svg, "image/svg+xml; charset=utf-8");
    }
}
