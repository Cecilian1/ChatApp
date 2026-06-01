using ChatApp.Shared.Enums;
using ChatApp.Shared.Mock;
using ChatApp.Shared.Models;

namespace ChatApp.Web.Services;

public interface IUserAccountService
{
    (bool Success, string? Error, UserDto? User) Login(string username, string password);
    (bool Success, string? Error) Register(RegisterRequestDto request);
    UserDto? GetUserById(string userId);
    void Logout();
    string? GetCurrentUserId(HttpContext httpContext);
    UserDto? GetCurrentUser(HttpContext httpContext);
}

public class MockUserAccountService : IUserAccountService
{
    public const string SessionUserIdKey = "CurrentUserId";

    public (bool Success, string? Error, UserDto? User) Login(string username, string password)
    {
        var data = MockDataStore.Load();
        var user = data.Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user is null)
            return (false, "账号不存在", null);

        if (user.Password != password)
            return (false, "密码错误", null);

        if (user.Status == UserStatus.Pending)
            return (false, "账号尚未通过管理员审核，请等待审批", null);

        if (user.Status == UserStatus.Banned)
            return (false, "账号已被禁用，请联系管理员", null);

        return (true, null, user);
    }

    public (bool Success, string? Error) Register(RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Nickname))
            return (false, "请填写完整信息");

        return MockDataStore.Mutate<(bool, string?)>(data =>
        {
            if (data.Users.Any(u => u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
                return (false, "该账号已被注册");

            data.Users.Add(new UserDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = request.Username.Trim(),
                Password = request.Password,
                Nickname = request.Nickname.Trim(),
                Remark = request.Remark?.Trim(),
                AvatarSeed = request.Username.Trim(),
                Status = UserStatus.Pending,
                RegisteredAt = DateTime.Now
            });

            return (true, null);
        });
    }

    public UserDto? GetUserById(string userId)
    {
        return MockDataStore.Load().Users.FirstOrDefault(u => u.Id == userId);
    }

    public void Logout() { }

    public string? GetCurrentUserId(HttpContext httpContext) =>
        httpContext.Session.GetString(SessionUserIdKey);

    public UserDto? GetCurrentUser(HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);
        return userId is null ? null : GetUserById(userId);
    }
}
