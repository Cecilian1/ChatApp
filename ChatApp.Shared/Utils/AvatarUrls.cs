namespace ChatApp.Shared.Utils;

public static class AvatarUrls
{
    public static string GetSvgUrl(string apiBase, string? seed, bool isGroup = false)
    {
        var baseUrl = apiBase.TrimEnd('/');
        var normalized = string.IsNullOrWhiteSpace(seed) ? "default" : seed.Trim();
        return $"{baseUrl}/api/avatar/svg?seed={Uri.EscapeDataString(normalized)}&group={(isGroup ? "true" : "false")}";
    }
}
