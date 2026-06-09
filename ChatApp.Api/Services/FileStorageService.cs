using ChatApp.Api.Options;
using Microsoft.Extensions.Options;

namespace ChatApp.Api.Services;

public interface IFileStorageService
{
    Task<(bool Success, string? Error, string? FileName, string? Url, long Size)> SaveAsync(IFormFile file);
}

public class FileStorageService(IWebHostEnvironment env, IOptions<FileStorageOptions> options) : IFileStorageService
{
    public async Task<(bool Success, string? Error, string? FileName, string? Url, long Size)> SaveAsync(IFormFile file)
    {
        var opt = options.Value;
        if (file.Length == 0) return (false, "未选择文件", null, null, 0);
        if (file.Length > opt.MaxSizeMb * 1024L * 1024L)
            return (false, $"文件大小不能超过 {opt.MaxSizeMb}MB", null, null, 0);

        var root = Path.Combine(env.ContentRootPath, opt.RootPath);
        Directory.CreateDirectory(root);
        var safeName = Path.GetFileName(file.FileName);
        var stored = $"{Guid.NewGuid():N}_{safeName}";
        var path = Path.Combine(root, stored);
        await using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        return (true, null, safeName, $"/uploads/{stored}", file.Length);
    }
}
