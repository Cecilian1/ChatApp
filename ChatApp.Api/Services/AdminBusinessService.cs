using ChatApp.Api.Data;
using ChatApp.Api.Data.Entities;
using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services;

public interface IAdminBusinessService
{
    Task<PendingRegistrationsResponse> GetPendingRegistrationsAsync();
    Task<List<UserDto>> GetUsersAsync(string? keyword, UserStatus? status);
    Task<List<MessageDto>> QueryMessagesAsync(MessageQueryFilter filter);
    Task<bool> ApproveRegistrationAsync(long userId);
    Task<bool> RejectRegistrationAsync(long userId);
    Task<bool> BanUserAsync(long userId);
    Task<bool> UnbanUserAsync(long userId);
    Task<bool> DeleteMessageAsync(long messageId);
    Task<bool> DeleteMessagesAsync(IEnumerable<long> messageIds);
}

public class AdminBusinessService(AppDbContext db) : IAdminBusinessService
{
    public async Task<PendingRegistrationsResponse> GetPendingRegistrationsAsync()
    {
        var items = await db.Users
            .Where(u => u.Status == UserStatus.Pending)
            .OrderByDescending(u => u.RegisteredAt)
            .Select(u => AuthService.MapUser(u))
            .ToListAsync();
        return new PendingRegistrationsResponse { Count = items.Count, Items = items };
    }

    public async Task<List<UserDto>> GetUsersAsync(string? keyword, UserStatus? status)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u => u.Username.Contains(keyword) || u.Nickname.Contains(keyword));
        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);
        else
            query = query.Where(u => u.Status != UserStatus.Pending);

        return await query.OrderByDescending(u => u.RegisteredAt)
            .Select(u => AuthService.MapUser(u)).ToListAsync();
    }

    public async Task<List<MessageDto>> QueryMessagesAsync(MessageQueryFilter filter)
    {
        var query = db.Messages.Include(m => m.Sender).Include(m => m.Conversation).ThenInclude(c => c!.Group)
            .Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SenderUsername))
            query = query.Where(m => m.Sender.Username.Contains(filter.SenderUsername));

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(m => m.Content.Contains(filter.Keyword));

        query = filter.TimeRange switch
        {
            "today" => query.Where(m => m.SentAt.Date == DateTime.UtcNow.Date),
            "3days" => query.Where(m => m.SentAt >= DateTime.UtcNow.AddDays(-3)),
            "week" => query.Where(m => m.SentAt >= DateTime.UtcNow.AddDays(-7)),
            _ => query
        };

        var list = await query.OrderByDescending(m => m.SentAt).Take(200).ToListAsync();
        return list.Select(m => MapMessage(m, 0)).ToList();
    }

    public async Task<bool> ApproveRegistrationAsync(long userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Status == UserStatus.Pending);
        if (user is null) return false;
        user.Status = UserStatus.Active;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectRegistrationAsync(long userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Status == UserStatus.Pending);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BanUserAsync(long userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Status == UserStatus.Active);
        if (user is null) return false;
        user.Status = UserStatus.Banned;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnbanUserAsync(long userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Status == UserStatus.Banned);
        if (user is null) return false;
        user.Status = UserStatus.Active;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMessageAsync(long messageId)
    {
        var msg = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (msg is null) return false;
        msg.IsDeleted = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMessagesAsync(IEnumerable<long> messageIds)
    {
        var ids = messageIds.ToHashSet();
        var msgs = await db.Messages.Where(m => ids.Contains(m.Id)).ToListAsync();
        if (msgs.Count == 0) return false;
        foreach (var m in msgs) m.IsDeleted = true;
        await db.SaveChangesAsync();
        return true;
    }

    internal static MessageDto MapMessage(Message m, long currentUserId) => new()
    {
        Id = m.Id.ToString(),
        SessionId = m.ConversationId.ToString(),
        SenderId = m.SenderId.ToString(),
        SenderName = m.Sender?.Nickname ?? "",
        SenderAvatarSeed = m.Sender?.AvatarSeed ?? "",
        ReceiverName = m.Conversation?.Type == SessionType.Group
            ? m.Conversation.Group?.Name
            : "私聊",
        Type = m.Type,
        Content = m.Content,
        FileName = m.FileName,
        FileSize = m.FileSize,
        FileProgress = m.Type == MessageType.File ? 100 : null,
        SentAt = m.SentAt,
        IsMine = m.SenderId == currentUserId
    };

    private static string FormatSize(long bytes) =>
        bytes < 1024 * 1024 ? $"{bytes / 1024.0:F1} KB" : $"{bytes / 1024.0 / 1024.0:F1} MB";
}
