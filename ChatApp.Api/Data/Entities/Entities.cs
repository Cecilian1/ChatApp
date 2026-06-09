using ChatApp.Shared.Enums;

namespace ChatApp.Api.Data.Entities;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = "Felix";
    public UserStatus Status { get; set; }
    public string? Remark { get; set; }
    public string OnlineStatus { get; set; } = "离线";
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<FriendRequest> SentRequests { get; set; } = [];
    public ICollection<FriendRequest> ReceivedRequests { get; set; } = [];
    public ICollection<Friendship> Friendships { get; set; } = [];
    public ICollection<GroupMember> GroupMembers { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}

public class Admin
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "超级管理员";
}

public class FriendRequest
{
    public long Id { get; set; }
    public long FromUserId { get; set; }
    public long ToUserId { get; set; }
    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}

public class Friendship
{
    public long UserId { get; set; }
    public long FriendId { get; set; }
    public User User { get; set; } = null!;
    public User Friend { get; set; } = null!;
}

public class ChatGroup
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;
    public long CreatorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User Creator { get; set; } = null!;
    public ICollection<GroupMember> Members { get; set; } = [];
    public Conversation? Conversation { get; set; }
}

public class GroupMember
{
    public long GroupId { get; set; }
    public long UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public ChatGroup Group { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class Conversation
{
    public long Id { get; set; }
    public SessionType Type { get; set; }
    public long? UserAId { get; set; }
    public long? UserBId { get; set; }
    public long? GroupId { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public User? UserA { get; set; }
    public User? UserB { get; set; }
    public ChatGroup? Group { get; set; }
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<ConversationRead> Reads { get; set; } = [];
}

public class Message
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
}

public class ConversationRead
{
    public long UserId { get; set; }
    public long ConversationId { get; set; }
    public long LastReadMessageId { get; set; }
    public User User { get; set; } = null!;
    public Conversation Conversation { get; set; } = null!;
}

public class UserMessageHide
{
    public long UserId { get; set; }
    public long MessageId { get; set; }
}
