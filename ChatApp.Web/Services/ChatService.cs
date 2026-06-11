using ChatApp.Shared.Enums;
using ChatApp.Shared.Mock;
using ChatApp.Shared.Models;

namespace ChatApp.Web.Services;

public interface IChatService
{
    List<ChatSessionDto> GetSessions(string userId);
    List<MessageDto> GetMessages(string userId, string sessionId);
    MessageDto SendMessage(string userId, string sessionId, string content);
    MessageDto SendFileMessage(string userId, string sessionId, string fileName, long fileSizeBytes, int progress, string? fileUrl = null);
    List<MessageDto> QueryHistory(string userId, MessageQueryFilter filter);
    bool DeleteMessage(string userId, string messageId);
    bool DeleteMessages(string userId, IEnumerable<string> messageIds);
    ChatSessionDto? GetSession(string userId, string sessionId);
}

public class MockChatService : IChatService
{
    public List<ChatSessionDto> GetSessions(string userId)
    {
        var data = MockDataStore.Load();
        var sessions = new List<ChatSessionDto>();

        var friendIds = data.Friendships
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendId)
            .ToList();

        foreach (var friendId in friendIds)
        {
            var friend = data.Users.FirstOrDefault(u => u.Id == friendId);
            if (friend is null) continue;

            var sessionId = MockDataStore.GetPrivateSessionId(userId, friendId);
            var msgs = data.Messages.Where(m => m.SessionId == sessionId).OrderByDescending(m => m.SentAt).ToList();
            var last = msgs.FirstOrDefault();

            sessions.Add(new ChatSessionDto
            {
                Id = sessionId,
                Type = SessionType.Private,
                Title = friend.Nickname,
                AvatarSeed = friend.AvatarSeed,
                TargetUserId = friendId,
                LastMessage = last?.Type == MessageType.File ? $"[文件] {last.FileName}" : last?.Content,
                LastMessageTime = last?.SentAt,
                UnreadCount = msgs.Count(m => !m.IsMine && m.SentAt > DateTime.Now.AddHours(-1)),
                OnlineStatus = friend.OnlineStatus
            });
        }

        var userGroupIds = data.Groups.Where(g => g.MemberIds.Contains(userId)).Select(g => g.Id);
        foreach (var groupId in userGroupIds)
        {
            var group = data.Groups.First(g => g.Id == groupId);
            var sessionId = MockDataStore.GetGroupSessionId(groupId);
            var msgs = data.Messages.Where(m => m.SessionId == sessionId).OrderByDescending(m => m.SentAt).ToList();
            var last = msgs.FirstOrDefault();

            sessions.Add(new ChatSessionDto
            {
                Id = sessionId,
                Type = SessionType.Group,
                Title = group.Name,
                AvatarSeed = group.AvatarSeed,
                GroupId = groupId,
                LastMessage = last is null ? null : $"{last.SenderName}：{last.Content}",
                LastMessageTime = last?.SentAt,
                UnreadCount = 0,
                OnlineStatus = $"{group.MemberIds.Count} 人"
            });
        }

        return sessions.OrderByDescending(s => s.LastMessageTime ?? DateTime.MinValue).ToList();
    }

    public List<MessageDto> GetMessages(string userId, string sessionId)
    {
        var data = MockDataStore.Load();
        return data.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.SentAt)
            .Select(m => MapMessage(m, userId))
            .ToList();
    }

    public MessageDto SendMessage(string userId, string sessionId, string content)
    {
        return MockDataStore.Mutate(data =>
        {
            var user = data.Users.First(u => u.Id == userId);
            var msg = new MessageDto
            {
                Id = Guid.NewGuid().ToString("N"),
                SessionId = sessionId,
                SenderId = userId,
                SenderName = user.Nickname,
                SenderAvatarSeed = user.AvatarSeed,
                Type = MessageType.Text,
                Content = content,
                SentAt = DateTime.Now,
                IsMine = true
            };
            data.Messages.Add(msg);
            return MapMessage(msg, userId);
        });
    }

    public MessageDto SendFileMessage(string userId, string sessionId, string fileName, long fileSizeBytes, int progress, string? fileUrl = null)
    {
        return MockDataStore.Mutate(data =>
        {
            var user = data.Users.First(u => u.Id == userId);
            var msg = new MessageDto
            {
                Id = Guid.NewGuid().ToString("N"),
                SessionId = sessionId,
                SenderId = userId,
                SenderName = user.Nickname,
                SenderAvatarSeed = user.AvatarSeed,
                Type = MessageType.File,
                Content = fileUrl ?? fileName,
                FileName = fileName,
                FileSize = fileSizeBytes,
                FileProgress = progress,
                SentAt = DateTime.Now,
                IsMine = true
            };
            data.Messages.Add(msg);
            return MapMessage(msg, userId);
        });
    }

    public List<MessageDto> QueryHistory(string userId, MessageQueryFilter filter)
    {
        var data = MockDataStore.Load();
        var sessionIds = GetSessions(userId).Select(s => s.Id).ToHashSet();

        var query = data.Messages
            .Where(m => sessionIds.Contains(m.SessionId))
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(m => m.Content.Contains(filter.Keyword, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.SenderUsername))
        {
            var senderIds = data.Users
                .Where(u => u.Username.Contains(filter.SenderUsername, StringComparison.OrdinalIgnoreCase))
                .Select(u => u.Id)
                .ToHashSet();
            query = query.Where(m => senderIds.Contains(m.SenderId));
        }

        query = filter.TimeRange switch
        {
            "today" => query.Where(m => m.SentAt.Date == DateTime.Today),
            "3days" => query.Where(m => m.SentAt >= DateTime.Now.AddDays(-3)),
            "week" => query.Where(m => m.SentAt >= DateTime.Now.AddDays(-7)),
            _ => query
        };

        return query
            .OrderByDescending(m => m.SentAt)
            .Select(m => MapMessage(m, userId))
            .ToList();
    }

    public bool DeleteMessage(string userId, string messageId)
    {
        return MockDataStore.Mutate(data =>
        {
            var msg = data.Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg is null) return false;
            var sessionIds = GetSessions(userId).Select(s => s.Id).ToHashSet();
            if (!sessionIds.Contains(msg.SessionId)) return false;
            data.Messages.Remove(msg);
            return true;
        });
    }

    public bool DeleteMessages(string userId, IEnumerable<string> messageIds)
    {
        var ids = messageIds.ToHashSet();
        return MockDataStore.Mutate(data =>
        {
            var sessionIds = GetSessions(userId).Select(s => s.Id).ToHashSet();
            var toRemove = data.Messages
                .Where(m => ids.Contains(m.Id) && sessionIds.Contains(m.SessionId))
                .ToList();
            foreach (var msg in toRemove)
                data.Messages.Remove(msg);
            return toRemove.Count > 0;
        });
    }

    public ChatSessionDto? GetSession(string userId, string sessionId) =>
        GetSessions(userId).FirstOrDefault(s => s.Id == sessionId);

    private static MessageDto MapMessage(MessageDto m, string userId) => new()
    {
        Id = m.Id,
        SessionId = m.SessionId,
        SenderId = m.SenderId,
        SenderName = m.SenderName,
        SenderAvatarSeed = m.SenderAvatarSeed,
        ReceiverName = m.ReceiverName,
        Type = m.Type,
        Content = m.Content,
        FileName = m.FileName,
        FileSize = m.FileSize,
        FileProgress = m.FileProgress,
        SentAt = m.SentAt,
        IsMine = m.SenderId == userId
    };
}