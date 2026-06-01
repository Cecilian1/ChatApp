using ChatApp.Shared.Enums;

namespace ChatApp.Shared.Models;

public class ChatSessionDto
{
    public string Id { get; set; } = string.Empty;
    public SessionType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public string? TargetUserId { get; set; }
    public string? GroupId { get; set; }
    public string OnlineStatus { get; set; } = string.Empty;
}
