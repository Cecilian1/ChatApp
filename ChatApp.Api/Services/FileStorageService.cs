using ChatApp.Api.Options;
using Microsoft.Extensions.Options;

namespace ChatApp.Api.Services;

public interface IFileStorageService
{
    Task<(bool Success, string? Error, string? FileName, string? Url, long Size)> SaveAsync(IFormFile file);
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly FileStorageOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FileStorageService(IWebHostEnvironment env, IOptions<FileStorageOptions> options, IHttpContextAccessor httpContextAccessor)
    {
        _env = env;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<(bool Success, string? Error, string? FileName, string? Url, long Size)> SaveAsync(IFormFile file)
    {
        if (file.Length == 0) return (false, "未选择文件", null, null, 0);
        if (file.Length > _options.MaxSizeMb * 1024L * 1024L)
            return (false, $"文件大小不能超过 {_options.MaxSizeMb}MB", null, null, 0);

        var root = Path.Combine(_env.ContentRootPath, _options.RootPath);
        Directory.CreateDirectory(root);
        var safeName = Path.GetFileName(file.FileName);
        var stored = $"{Guid.NewGuid():N}_{safeName}";
        var path = Path.Combine(root, stored);
        await using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = $"{request?.Scheme}://{request?.Host}";
        var url = $"{baseUrl}/uploads/{stored}";

        return (true, null, safeName, url, file.Length);
    }
}