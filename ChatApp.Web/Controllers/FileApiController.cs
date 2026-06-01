using ChatApp.Web.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireLogin]
public class FileApiController : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { success = false, error = "未选择文件" });

        await using var stream = file.OpenReadStream();
        await Task.Delay(500);

        var size = file.Length < 1024 * 1024
            ? $"{file.Length / 1024.0:F1} KB"
            : $"{file.Length / 1024.0 / 1024.0:F1} MB";

        return Ok(new
        {
            success = true,
            fileName = file.FileName,
            fileSize = size,
            url = $"/uploads/{Guid.NewGuid():N}_{file.FileName}"
        });
    }
}
