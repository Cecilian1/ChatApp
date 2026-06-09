using ChatApp.Shared.Models;
using ChatApp.Web.Services.Http;

namespace ChatApp.Web.Services;

public class HttpUserAccountService(ApiHttpClient api, IHttpContextAccessor httpContextAccessor) : IUserAccountService
{
    public (bool Success, string? Error, UserDto? User) Login(string username, string password)
    {
        try
        {
            var response = api.PostAsync<LoginResponse>("api/auth/login", new { username, password }).GetAwaiter().GetResult();
            if (response?.User is null || string.IsNullOrEmpty(response.Token))
                return (false, "登录失败", null);

            var ctx = httpContextAccessor.HttpContext!;
            ctx.Session.SetString(SessionKeys.JwtToken, response.Token);
            ctx.Session.SetString(SessionKeys.UserId, response.User.Id);
            return (true, null, response.User);
        }
        catch (ApiException ex)
        {
            return (false, ex.Message, null);
        }
    }

    public (bool Success, string? Error) Register(RegisterRequestDto request)
    {
        try
        {
            api.PostAsync("api/auth/register", request).GetAwaiter().GetResult();
            return (true, null);
        }
        catch (ApiException ex)
        {
            return (false, ex.Message);
        }
    }

    public UserDto? GetUserById(string userId) => GetCurrentUser(httpContextAccessor.HttpContext!);

    public void Logout()
    {
        var ctx = httpContextAccessor.HttpContext;
        ctx?.Session.Clear();
    }

    public string? GetCurrentUserId(HttpContext httpContext) =>
        httpContext.Session.GetString(SessionKeys.UserId);

    public UserDto? GetCurrentUser(HttpContext httpContext)
    {
        if (GetCurrentUserId(httpContext) is null) return null;
        try
        {
            return api.GetAsync<UserDto>("api/auth/me").GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }
}
