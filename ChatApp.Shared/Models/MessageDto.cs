using ChatApp.Shared.Enums;

namespace ChatApp.Shared.Models;

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderAvatarSeed { get; set; } = string.Empty;
    public string? ReceiverName { get; set; }
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileSize { get; set; }
    public int? FileProgress { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsMine { get; set; }
}
