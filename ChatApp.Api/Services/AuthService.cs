using ChatApp.Api.Data;
using ChatApp.Api.Data.Entities;
using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error)> RegisterAsync(RegisterRequestDto request);
    Task<(bool Success, string? Error, LoginResponse? Response)> LoginAsync(string username, string password);
    Task<(bool Success, string? Error, LoginResponse? Response)> AdminLoginAsync(string username, string password);
}

public class AuthService(AppDbContext db, ITokenService tokens) : IAuthService
{
    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Nickname))
            return (false, "请填写完整信息");

        if (await db.Users.AnyAsync(u => u.Username == request.Username.Trim()))
            return (false, "该账号已被注册");

        db.Users.Add(new User
        {
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Nickname = request.Nickname.Trim(),
            Remark = request.Remark?.Trim(),
            AvatarSeed = request.Username.Trim(),
            Status = UserStatus.Pending,
            RegisteredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, LoginResponse? Response)> LoginAsync(string username, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "账号或密码错误", null);

        if (user.Status == UserStatus.Pending)
            return (false, "账号尚未通过管理员审核，请等待审批", null);
        if (user.Status == UserStatus.Banned)
            return (false, "账号已被禁用，请联系管理员", null);

        user.OnlineStatus = "在线";
        await db.SaveChangesAsync();

        return (true, null, new LoginResponse
        {
            Token = tokens.CreateUserToken(user.Id, user.Username),
            User = MapUser(user)
        });
    }

    public async Task<(bool Success, string? Error, LoginResponse? Response)> AdminLoginAsync(string username, string password)
    {
        var admin = await db.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (admin is null || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            return (false, "账号或密码错误", null);

        return (true, null, new LoginResponse
        {
            Token = tokens.CreateAdminToken(admin.Id, admin.Username),
            AdminName = admin.DisplayName
        });
    }

    internal static UserDto MapUser(User u) => new()
    {
        Id = u.Id.ToString(),
        Username = u.Username,
        Nickname = u.Nickname,
        AvatarSeed = u.AvatarSeed,
        Status = u.Status,
        Remark = u.Remark,
        RegisteredAt = u.RegisteredAt,
        OnlineStatus = u.OnlineStatus
    };
}
