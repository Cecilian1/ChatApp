using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatApp.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Api.Services;

public interface ITokenService
{
    string CreateUserToken(long userId, string username);
    string CreateAdminToken(long adminId, string username);
}

public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _opt = options.Value;

    public string CreateUserToken(long userId, string username) =>
        CreateToken(userId.ToString(), username, "user", _opt.UserSecret);

    public string CreateAdminToken(long adminId, string username) =>
        CreateToken(adminId.ToString(), username, "admin", _opt.UserSecret);

    private string CreateToken(string subject, string username, string role, string secret)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_opt.ExpireHours),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class ClaimsExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.Parse(id!);
    }

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole("admin");
}
