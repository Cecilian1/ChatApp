namespace ChatApp.Shared.Models;

public class FriendDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;
    public string OnlineStatus { get; set; } = "离线";
}
