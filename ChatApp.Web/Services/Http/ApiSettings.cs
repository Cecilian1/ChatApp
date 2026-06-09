namespace ChatApp.Web.Services.Http;

public static class SessionKeys
{
    public const string UserId = "CurrentUserId";
    public const string JwtToken = "JwtToken";
    public const string AdminJwtToken = "AdminJwtToken";
}

public class ApiSettings
{
    public const string SectionName = "Api";
    public string BaseUrl { get; set; } = "http://localhost:5200";
}
