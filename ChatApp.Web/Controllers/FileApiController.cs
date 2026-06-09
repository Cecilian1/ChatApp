using ChatApp.Web.Filters;
using ChatApp.Web.Services.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

[Route("api/File")]
[ApiController]
[RequireLogin]
public class FileApiController(ApiHttpClient api) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { success = false, error = "未选择文件" });

        try
        {
            var result = await api.UploadAsync<UploadResult>("api/file/upload", file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    private class UploadResult
    {
        public bool Success { get; set; }
        public string? FileName { get; set; }
        public string? FileSize { get; set; }
        public string? Url { get; set; }
        public long SizeBytes { get; set; }
    }
}
