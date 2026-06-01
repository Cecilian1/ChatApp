using ChatApp.Shared.Enums;

namespace ChatApp.Shared.Models;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = "Felix";
    public UserStatus Status { get; set; }
    public string? Remark { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string OnlineStatus { get; set; } = "离线";
}
