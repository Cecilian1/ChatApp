using ChatApp.Shared.Enums;

namespace ChatApp.Shared.Models;

public class FriendRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string FromUserId { get; set; } = string.Empty;
    public string FromUsername { get; set; } = string.Empty;
    public string FromNickname { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
