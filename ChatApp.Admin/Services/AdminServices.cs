using ChatApp.Shared.Enums;
using ChatApp.Shared.Mock;
using ChatApp.Shared.Models;

namespace ChatApp.Admin.Services;

public interface IAdminAuthService
{
    bool IsLoggedIn { get; }
    bool Login(string username, string password);
    void Logout();
    event Action? OnChange;
}

public class AdminAuthService : IAdminAuthService
{
    public bool IsLoggedIn { get; private set; }

    public event Action? OnChange;

    public bool Login(string username, string password)
    {
        if (username == "admin" && password == "admin123")
        {
            IsLoggedIn = true;
            OnChange?.Invoke();
            return true;
        }
        return false;
    }

    public void Logout()
    {
        IsLoggedIn = false;
        OnChange?.Invoke();
    }
}

public interface IAdminService
{
    int GetPendingCount();
    List<UserDto> GetPendingRegistrations();
    List<UserDto> GetAllUsers(string? keyword, UserStatus? status);
    List<MessageDto> QueryMessages(MessageQueryFilter filter);
    bool ApproveRegistration(string userId);
    bool RejectRegistration(string userId);
    bool BanUser(string userId);
    bool UnbanUser(string userId);
    bool DeleteMessage(string messageId);
    bool DeleteMessages(IEnumerable<string> messageIds);
}

public class MockAdminService : IAdminService
{
    public int GetPendingCount() =>
        MockDataStore.Load().Users.Count(u => u.Status == UserStatus.Pending);

    public List<UserDto> GetPendingRegistrations() =>
        MockDataStore.Load().Users
            .Where(u => u.Status == UserStatus.Pending)
            .OrderByDescending(u => u.RegisteredAt)
            .ToList();

    public List<UserDto> GetAllUsers(string? keyword, UserStatus? status)
    {
        var query = MockDataStore.Load().Users.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u =>
                u.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                u.Nickname.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);
        else
            query = query.Where(u => u.Status != UserStatus.Pending);

        return query.OrderByDescending(u => u.RegisteredAt).ToList();
    }

    public List<MessageDto> QueryMessages(MessageQueryFilter filter)
    {
        var data = MockDataStore.Load();
        var query = data.Messages.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.SenderUsername))
        {
            var senderIds = data.Users
                .Where(u => u.Username.Contains(filter.SenderUsername, StringComparison.OrdinalIgnoreCase))
                .Select(u => u.Id)
                .ToHashSet();
            query = query.Where(m => senderIds.Contains(m.SenderId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(m => m.Content.Contains(filter.Keyword, StringComparison.OrdinalIgnoreCase));

        query = filter.TimeRange switch
        {
            "today" => query.Where(m => m.SentAt.Date == DateTime.Today),
            "3days" => query.Where(m => m.SentAt >= DateTime.Now.AddDays(-3)),
            "week" => query.Where(m => m.SentAt >= DateTime.Now.AddDays(-7)),
            _ => query
        };

        return query.OrderByDescending(m => m.SentAt).Select(m => new MessageDto
        {
            Id = m.Id,
            SessionId = m.SessionId,
            SenderId = m.SenderId,
            SenderName = m.SenderName,
            Type = m.Type,
            Content = m.Content,
            FileName = m.FileName,
            SentAt = m.SentAt,
            ReceiverName = GetReceiverName(data, m)
        }).ToList();
    }

    public bool ApproveRegistration(string userId) =>
        MockDataStore.Mutate(data =>
        {
            var user = data.Users.FirstOrDefault(u => u.Id == userId && u.Status == UserStatus.Pending);
            if (user is null) return false;
            user.Status = UserStatus.Active;
            return true;
        });

    public bool RejectRegistration(string userId) =>
        MockDataStore.Mutate(data =>
        {
            var user = data.Users.FirstOrDefault(u => u.Id == userId && u.Status == UserStatus.Pending);
            if (user is null) return false;
            data.Users.Remove(user);
            return true;
        });

    public bool BanUser(string userId) =>
        MockDataStore.Mutate(data =>
        {
            var user = data.Users.FirstOrDefault(u => u.Id == userId && u.Status == UserStatus.Active);
            if (user is null) return false;
            user.Status = UserStatus.Banned;
            return true;
        });

    public bool UnbanUser(string userId) =>
        MockDataStore.Mutate(data =>
        {
            var user = data.Users.FirstOrDefault(u => u.Id == userId && u.Status == UserStatus.Banned);
            if (user is null) return false;
            user.Status = UserStatus.Active;
            return true;
        });

    public bool DeleteMessage(string messageId) =>
        MockDataStore.Mutate(data =>
        {
            var msg = data.Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg is null) return false;
            data.Messages.Remove(msg);
            return true;
        });

    public bool DeleteMessages(IEnumerable<string> messageIds)
    {
        var ids = messageIds.ToHashSet();
        return MockDataStore.Mutate(data =>
        {
            var removed = data.Messages.RemoveAll(m => ids.Contains(m.Id));
            return removed > 0;
        });
    }

    private static string GetReceiverName(MockDataSnapshot data, MessageDto m)
    {
        if (!string.IsNullOrEmpty(m.ReceiverName))
            return m.ReceiverName;

        if (m.SessionId.StartsWith("session-group-"))
        {
            var groupId = m.SessionId.Replace("session-group-", "");
            return data.Groups.FirstOrDefault(g => g.Id == groupId)?.Name ?? "群聊";
        }

        return "私聊";
    }
}
