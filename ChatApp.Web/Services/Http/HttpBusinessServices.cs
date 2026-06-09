using ChatApp.Shared.Models;
using ChatApp.Web.Services.Http;

namespace ChatApp.Web.Services;

public class HttpChatService(ApiHttpClient api) : IChatService
{
    public List<ChatSessionDto> GetSessions(string userId) =>
        api.GetAsync<List<ChatSessionDto>>("api/chat/sessions").GetAwaiter().GetResult() ?? [];

    public List<MessageDto> GetMessages(string userId, string sessionId) =>
        api.GetAsync<List<MessageDto>>($"api/chat/messages/{sessionId}").GetAwaiter().GetResult() ?? [];

    public MessageDto SendMessage(string userId, string sessionId, string content) =>
        api.PostAsync<MessageDto>("api/chat/send", new { sessionId, content }).GetAwaiter().GetResult()!;

    public MessageDto SendFileMessage(string userId, string sessionId, string fileName, string fileSize, int progress, string? fileUrl = null) =>
        api.PostAsync<MessageDto>("api/chat/send-file", new
        {
            sessionId,
            fileName,
            content = fileUrl ?? fileName,
            fileSizeBytes = ParseSize(fileSize)
        }).GetAwaiter().GetResult()!;

    public List<MessageDto> QueryHistory(string userId, MessageQueryFilter filter)
    {
        var q = $"api/chat/history?keyword={Uri.EscapeDataString(filter.Keyword ?? "")}&timeRange={filter.TimeRange}";
        return api.GetAsync<List<MessageDto>>(q).GetAwaiter().GetResult() ?? [];
    }

    public bool DeleteMessage(string userId, string messageId)
    {
        api.DeleteAsync($"api/chat/message/{messageId}").GetAwaiter().GetResult();
        return true;
    }

    public bool DeleteMessages(string userId, IEnumerable<string> messageIds)
    {
        api.PostAsync("api/chat/messages/delete", new { messageIds = messageIds.ToList() }).GetAwaiter().GetResult();
        return true;
    }

    public ChatSessionDto? GetSession(string userId, string sessionId) =>
        api.GetAsync<ChatSessionDto>($"api/chat/session/{sessionId}").GetAwaiter().GetResult();

    private static long ParseSize(string size)
    {
        if (size.Contains("MB", StringComparison.OrdinalIgnoreCase))
            return (long)(double.Parse(size.Replace("MB", "").Trim()) * 1024 * 1024);
        if (size.Contains("KB", StringComparison.OrdinalIgnoreCase))
            return (long)(double.Parse(size.Replace("KB", "").Trim()) * 1024);
        return 0;
    }
}

public class HttpFriendService(ApiHttpClient api) : IFriendService
{
    public List<FriendDto> GetFriends(string userId) =>
        api.GetAsync<List<FriendDto>>("api/friend").GetAwaiter().GetResult() ?? [];

    public List<FriendRequestDto> GetPendingRequests(string userId) =>
        api.GetAsync<List<FriendRequestDto>>("api/friend/requests/pending").GetAwaiter().GetResult() ?? [];

    public List<FriendRequestDto> GetSentRequests(string userId) => [];

    public List<UserDto> SearchUsers(string userId, string keyword) =>
        api.GetAsync<List<UserDto>>($"api/friend/search?keyword={Uri.EscapeDataString(keyword)}").GetAwaiter().GetResult() ?? [];

    public (bool Success, string? Error) SendRequest(string userId, string targetUserId)
    {
        try
        {
            api.PostAsync("api/friend/send-friend-request", new { targetUserId }).GetAwaiter().GetResult();
            return (true, null);
        }
        catch (ApiException ex) { return (false, ex.Message); }
    }

    public (bool Success, string? Error) AcceptRequest(string userId, string requestId)
    {
        try
        {
            api.PostAsync("api/friend/accept", new { requestId }).GetAwaiter().GetResult();
            return (true, null);
        }
        catch (ApiException ex) { return (false, ex.Message); }
    }

    public (bool Success, string? Error) RejectRequest(string userId, string requestId)
    {
        try
        {
            api.PostAsync("api/friend/reject", new { requestId }).GetAwaiter().GetResult();
            return (true, null);
        }
        catch (ApiException ex) { return (false, ex.Message); }
    }

    public (bool Success, string? Error) RemoveFriend(string userId, string friendId)
    {
        try
        {
            api.DeleteAsync($"api/friend/{friendId}").GetAwaiter().GetResult();
            return (true, null);
        }
        catch (ApiException ex) { return (false, ex.Message); }
    }
}

public class HttpGroupService(ApiHttpClient api) : IGroupService
{
    public (bool Success, string? Error, GroupDto? Group) CreateGroup(string userId, string name, List<string> memberIds)
    {
        try
        {
            var group = api.PostAsync<GroupDto>("api/group", new { name, memberIds }).GetAwaiter().GetResult();
            return (true, null, group);
        }
        catch (ApiException ex) { return (false, ex.Message, null); }
    }

    public List<GroupDto> GetGroups(string userId) =>
        api.GetAsync<List<GroupDto>>("api/group").GetAwaiter().GetResult() ?? [];
}
