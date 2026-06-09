namespace ChatApp.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "ChatApp";
    public string Audience { get; set; } = "ChatApp";
    public string UserSecret { get; set; } = "ChatApp-User-Secret-Key-2026-Min32Chars!";
    public string AdminSecret { get; set; } = "ChatApp-Admin-Secret-Key-2026-Min32Chars!";
    public int ExpireHours { get; set; } = 8;
}

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; set; } = "uploads";
    public int MaxSizeMb { get; set; } = 20;
}
