using ChatApp.Api.Data;
using ChatApp.Api.Data.Entities;
using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services;

public interface IChatBusinessService
{
    Task<List<ChatSessionDto>> GetSessionsAsync(long userId);
    Task<ChatSessionDto?> GetSessionAsync(long userId, long conversationId);
    Task<List<MessageDto>> GetMessagesAsync(long userId, long conversationId);
    Task<MessageDto?> SendTextAsync(long userId, long conversationId, string content);
    Task<MessageDto?> SendFileAsync(long userId, long conversationId, string fileName, string filePath, long fileSize);
    Task<List<MessageDto>> QueryHistoryAsync(long userId, MessageQueryFilter filter);
    Task<bool> DeleteMessageAsync(long userId, long messageId);
    Task<bool> DeleteMessagesAsync(long userId, IEnumerable<long> messageIds);
    Task<bool> IsMemberAsync(long userId, long conversationId);
    Task<List<long>> GetMemberUserIdsAsync(long conversationId);
    Task MarkReadAsync(long userId, long conversationId, long messageId);
}

public class ChatBusinessService(AppDbContext db) : IChatBusinessService
{
    public async Task<List<ChatSessionDto>> GetSessionsAsync(long userId)
    {
        var sessions = new List<ChatSessionDto>();

        var friendships = await db.Friendships.Include(f => f.Friend).Where(f => f.UserId == userId).ToListAsync();
        foreach (var f in friendships)
        {
            var a = Math.Min(userId, f.FriendId);
            var b = Math.Max(userId, f.FriendId);
            var conv = await db.Conversations.FirstOrDefaultAsync(c => c.Type == SessionType.Private && c.UserAId == a && c.UserBId == b);
            if (conv is null)
            {
                conv = new Conversation { Type = SessionType.Private, UserAId = a, UserBId = b };
                db.Conversations.Add(conv);
                await db.SaveChangesAsync();
            }
            sessions.Add(await BuildSessionDto(conv, userId, f.Friend));
        }

        var groupConvs = await db.Conversations
            .Include(c => c.Group!).ThenInclude(g => g.Members)
            .Where(c => c.Type == SessionType.Group && c.Group!.Members.Any(m => m.UserId == userId))
            .ToListAsync();

        foreach (var conv in groupConvs)
            sessions.Add(await BuildGroupSessionDto(conv, userId));

        return sessions.OrderByDescending(s => s.LastMessageTime ?? DateTime.MinValue).ToList();
    }

    public async Task<ChatSessionDto?> GetSessionAsync(long userId, long conversationId)
    {
        if (!await IsMemberAsync(userId, conversationId)) return null;
        var conv = await db.Conversations.Include(c => c.Group).FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conv is null) return null;
        if (conv.Type == SessionType.Group)
            return await BuildGroupSessionDto(conv, userId);
        var friendId = conv.UserAId == userId ? conv.UserBId!.Value : conv.UserAId!.Value;
        var friend = await db.Users.FindAsync(friendId);
        return friend is null ? null : await BuildSessionDto(conv, userId, friend);
    }

    public async Task<List<MessageDto>> GetMessagesAsync(long userId, long conversationId)
    {
        if (!await IsMemberAsync(userId, conversationId)) return [];
        var hidden = await db.UserMessageHides.Where(h => h.UserId == userId).Select(h => h.MessageId).ToListAsync();
        var msgs = await db.Messages.Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted && !hidden.Contains(m.Id))
            .OrderBy(m => m.SentAt).ToListAsync();

        if (msgs.Count > 0)
            await MarkReadAsync(userId, conversationId, msgs.Last().Id);

        return msgs.Select(m => AdminBusinessService.MapMessage(m, userId)).ToList();
    }

    public async Task<MessageDto?> SendTextAsync(long userId, long conversationId, string content)
    {
        if (!await IsMemberAsync(userId, conversationId)) return null;
        var user = await db.Users.FindAsync(userId);
        var msg = new Message
        {
            ConversationId = conversationId,
            SenderId = userId,
            Type = MessageType.Text,
            Content = content,
            SentAt = DateTime.UtcNow
        };
        db.Messages.Add(msg);
        var conv = await db.Conversations.FindAsync(conversationId);
        if (conv is not null) conv.LastMessageAt = msg.SentAt;
        await db.SaveChangesAsync();
        msg.Sender = user!;
        return AdminBusinessService.MapMessage(msg, userId);
    }

    public async Task<MessageDto?> SendFileAsync(long userId, long conversationId, string fileName, string filePath, long fileSize)
    {
        if (!await IsMemberAsync(userId, conversationId)) return null;
        var user = await db.Users.FindAsync(userId);
        var msg = new Message
        {
            ConversationId = conversationId,
            SenderId = userId,
            Type = MessageType.File,
            Content = filePath,
            FileName = fileName,
            FilePath = filePath,
            FileSize = fileSize,
            SentAt = DateTime.UtcNow
        };
        db.Messages.Add(msg);
        var conv = await db.Conversations.FindAsync(conversationId);
        if (conv is not null) conv.LastMessageAt = msg.SentAt;
        await db.SaveChangesAsync();
        msg.Sender = user!;
        return AdminBusinessService.MapMessage(msg, userId);
    }

    public async Task<List<MessageDto>> QueryHistoryAsync(long userId, MessageQueryFilter filter)
    {
        var convIds = await GetUserConversationIdsAsync(userId);
        var hidden = await db.UserMessageHides.Where(h => h.UserId == userId).Select(h => h.MessageId).ToListAsync();
        var query = db.Messages.Include(m => m.Sender)
            .Where(m => convIds.Contains(m.ConversationId) && !m.IsDeleted && !hidden.Contains(m.Id));

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(m => m.Content.Contains(filter.Keyword));

        query = filter.TimeRange switch
        {
            "today" => query.Where(m => m.SentAt.Date == DateTime.UtcNow.Date),
            "3days" => query.Where(m => m.SentAt >= DateTime.UtcNow.AddDays(-3)),
            "week" => query.Where(m => m.SentAt >= DateTime.UtcNow.AddDays(-7)),
            _ => query
        };

        var list = await query.OrderByDescending(m => m.SentAt).Take(100).ToListAsync();
        return list.Select(m => AdminBusinessService.MapMessage(m, userId)).ToList();
    }

    public async Task<bool> DeleteMessageAsync(long userId, long messageId)
    {
        var msg = await db.Messages.FindAsync(messageId);
        if (msg is null || !await IsMemberAsync(userId, msg.ConversationId)) return false;
        db.UserMessageHides.Add(new UserMessageHide { UserId = userId, MessageId = messageId });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMessagesAsync(long userId, IEnumerable<long> messageIds)
    {
        foreach (var id in messageIds)
        {
            if (!await DeleteMessageAsync(userId, id)) continue;
        }
        return true;
    }

    public async Task<bool> IsMemberAsync(long userId, long conversationId)
    {
        var conv = await db.Conversations.Include(c => c.Group!).ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conv is null) return false;
        if (conv.Type == SessionType.Group)
            return conv.Group!.Members.Any(m => m.UserId == userId);
        return conv.UserAId == userId || conv.UserBId == userId;
    }

    public async Task<List<long>> GetMemberUserIdsAsync(long conversationId)
    {
        var conv = await db.Conversations.Include(c => c.Group!).ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conv is null) return [];
        if (conv.Type == SessionType.Group)
            return conv.Group!.Members.Select(m => m.UserId).ToList();
        return [conv.UserAId!.Value, conv.UserBId!.Value];
    }

    public async Task MarkReadAsync(long userId, long conversationId, long messageId)
    {
        var read = await db.ConversationReads.FindAsync(userId, conversationId);
        if (read is null)
            db.ConversationReads.Add(new ConversationRead { UserId = userId, ConversationId = conversationId, LastReadMessageId = messageId });
        else if (messageId > read.LastReadMessageId)
            read.LastReadMessageId = messageId;
        await db.SaveChangesAsync();
    }

    private async Task<List<long>> GetUserConversationIdsAsync(long userId)
    {
        var privateIds = await db.Conversations
            .Where(c => c.Type == SessionType.Private && (c.UserAId == userId || c.UserBId == userId))
            .Select(c => c.Id).ToListAsync();
        var groupIds = await db.GroupMembers
            .Where(m => m.UserId == userId)
            .Join(db.Conversations.Where(c => c.Type == SessionType.Group),
                m => m.GroupId, c => c.GroupId, (m, c) => c.Id).ToListAsync();
        return privateIds.Concat(groupIds).Distinct().ToList();
    }

    private async Task<ChatSessionDto> BuildSessionDto(Conversation conv, long userId, User friend)
    {
        var last = await db.Messages.Where(m => m.ConversationId == conv.Id && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt).FirstOrDefaultAsync();
        var read = await db.ConversationReads.FindAsync(userId, conv.Id);
        var unread = read is null
            ? await db.Messages.CountAsync(m => m.ConversationId == conv.Id && !m.IsDeleted && m.SenderId != userId)
            : await db.Messages.CountAsync(m => m.ConversationId == conv.Id && !m.IsDeleted && m.SenderId != userId && m.Id > read.LastReadMessageId);

        return new ChatSessionDto
        {
            Id = conv.Id.ToString(),
            Type = SessionType.Private,
            Title = friend.Nickname,
            AvatarSeed = friend.AvatarSeed,
            TargetUserId = friend.Id.ToString(),
            LastMessage = last?.Type == MessageType.File ? $"[文件] {last.FileName}" : last?.Content,
            LastMessageTime = last?.SentAt,
            UnreadCount = unread,
            OnlineStatus = friend.OnlineStatus
        };
    }

    private async Task<ChatSessionDto> BuildGroupSessionDto(Conversation conv, long userId)
    {
        var last = await db.Messages.Include(m => m.Sender)
            .Where(m => m.ConversationId == conv.Id && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt).FirstOrDefaultAsync();
        var memberCount = conv.Group?.Members.Count ?? 0;

        return new ChatSessionDto
        {
            Id = conv.Id.ToString(),
            Type = SessionType.Group,
            Title = conv.Group?.Name ?? "群聊",
            AvatarSeed = conv.Group?.AvatarSeed ?? "Group",
            GroupId = conv.GroupId?.ToString(),
            LastMessage = last is null ? null : $"{last.Sender?.Nickname}：{last.Content}",
            LastMessageTime = last?.SentAt,
            UnreadCount = 0,
            OnlineStatus = $"{memberCount} 人"
        };
    }
}
