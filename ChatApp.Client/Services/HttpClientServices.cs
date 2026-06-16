using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;

namespace ChatApp.Client.Services;

public class ApiSettings
{
    public const string SectionName = "Api";
    public string BaseUrl { get; set; } = "http://localhost:5200";
}

public class ApiException(string message) : Exception(message);

public class UserAuthState
{
    public string? Token { get; set; }
    public UserDto? User { get; set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token) && User is not null;
    public event Action? OnChange;
    public void Notify() => OnChange?.Invoke();
}

public class HttpApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly UserAuthState _auth;
    private readonly ApiSettings _settings;

    public HttpApiClient(HttpClient http, UserAuthState auth, ApiSettings settings)
    {
        _http = http;
        _auth = auth;
        _settings = settings;
        _http.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
    }

    public string ApiBaseUrl => _settings.BaseUrl.TrimEnd('/');

    public string ResolveUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;
        return ApiBaseUrl + (url.StartsWith('/') ? url : "/" + url);
    }

    public async Task<T?> GetAsync<T>(string path)
    {
        using var request = CreateRequest(HttpMethod.Get, path);
        var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task PostAsync(string path, object? body = null)
    {
        using var request = CreateRequest(HttpMethod.Post, path, body);
        var response = await _http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
    }

    public async Task<T?> PostAsync<T>(string path, object? body = null)
    {
        using var request = CreateRequest(HttpMethod.Post, path, body);
        var response = await _http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        if (response.Content.Headers.ContentLength == 0) return default;
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<T?> PutAsync<T>(string path, object? body = null)
    {
        using var request = CreateRequest(HttpMethod.Put, path, body);
        var response = await _http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task DeleteAsync(string path)
    {
        using var request = CreateRequest(HttpMethod.Delete, path);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<FileUploadResult?> UploadAsync(Stream stream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/file/upload") { Content = content };
        AttachToken(request);
        var response = await _http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<FileUploadResult>(JsonOptions);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        AttachToken(request);
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return request;
    }

    private void AttachToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_auth.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
    }

    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var err = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions);
        throw new ApiException(err?.Error ?? response.ReasonPhrase ?? "请求失败");
    }
}

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public string? Url { get; set; }
    public long SizeBytes { get; set; }
}

public interface IUserAuthService
{
    bool IsLoggedIn { get; }
    UserDto? CurrentUser { get; }
    event Action? OnChange;
    Task<bool> LoginAsync(string username, string password);
    Task<(bool Success, string? Error)> RegisterAsync(RegisterRequestDto request);
    void Logout();
}

public interface IChatAppService
{
    Task<List<ChatSessionDto>> GetSessionsAsync();
    Task<List<MessageDto>> GetMessagesAsync(string sessionId);
    Task<MessageDto?> SendMessageAsync(string sessionId, string content);
    Task<MessageDto?> SendFileMessageAsync(string sessionId, string fileName, long fileSizeBytes, string fileUrl);
    Task<List<MessageDto>> QueryHistoryAsync(MessageQueryFilter filter);
    Task<bool> DeleteMessageAsync(string messageId);
    Task<List<FriendDto>> GetFriendsAsync();
    Task<List<FriendRequestDto>> GetPendingRequestsAsync();
    Task<List<UserDto>> SearchUsersAsync(string keyword);
    Task<(bool Success, string? Error)> SendFriendRequestAsync(string targetUserId);
    Task<(bool Success, string? Error)> AcceptRequestAsync(string requestId);
    Task<(bool Success, string? Error)> RejectRequestAsync(string requestId);
    Task<(bool Success, string? Error)> RemoveFriendAsync(string friendId);
    Task<(bool Success, string? Error, GroupDto? Group)> CreateGroupAsync(string name, List<string> memberIds);
    Task<List<GroupMemberDto>> GetGroupMembersAsync(string groupId);
    Task<UserDto?> UpdateProfileAsync(string nickname, string avatarSeed);
    Task<FileUploadResult?> UploadFileAsync(Stream stream, string fileName);
    string ResolveUrl(string? url);
}

public class HttpUserAuthService : IUserAuthService
{
    private readonly HttpApiClient _api;
    private readonly UserAuthState _state;
    private readonly ChatHubService _hub;

    public HttpUserAuthService(HttpApiClient api, UserAuthState state, ChatHubService hub)
    {
        _api = api;
        _state = state;
        _hub = hub;
    }

    public bool IsLoggedIn => _state.IsLoggedIn;
    public UserDto? CurrentUser => _state.User;
    public event Action? OnChange
    {
        add => _state.OnChange += value;
        remove => _state.OnChange -= value;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _api.PostAsync<LoginResponse>("api/auth/login", new { username, password });
            if (response?.Token is null || response.User is null) return false;
            _state.Token = response.Token;
            _state.User = response.User;
            _state.Notify();
            await _hub.ConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            await _api.PostAsync("api/auth/register", request);
            return (true, null);
        }
        catch (ApiException ex)
        {
            return (false, ex.Message);
        }
    }

    public void Logout()
    {
        _ = _hub.DisconnectAsync();
        _state.Token = null;
        _state.User = null;
        _state.Notify();
    }
}

public class HttpChatAppService : IChatAppService
{
    private readonly HttpApiClient _api;

    public HttpChatAppService(HttpApiClient api) => _api = api;

    public string ResolveUrl(string? url) => _api.ResolveUrl(url);

    public Task<List<ChatSessionDto>> GetSessionsAsync() =>
        _api.GetAsync<List<ChatSessionDto>>("api/chat/sessions")!.ContinueWith(t => t.Result ?? []);

    public Task<List<MessageDto>> GetMessagesAsync(string sessionId) =>
        _api.GetAsync<List<MessageDto>>($"api/chat/messages/{sessionId}")!.ContinueWith(t => t.Result ?? []);

    public Task<MessageDto?> SendMessageAsync(string sessionId, string content) =>
        _api.PostAsync<MessageDto>("api/chat/send", new { sessionId, content });

    public Task<MessageDto?> SendFileMessageAsync(string sessionId, string fileName, long fileSizeBytes, string fileUrl) =>
        _api.PostAsync<MessageDto>("api/chat/send-file", new { sessionId, fileName, content = fileUrl, fileSizeBytes });

    public Task<List<MessageDto>> QueryHistoryAsync(MessageQueryFilter filter)
    {
        var q = $"api/chat/history?keyword={Uri.EscapeDataString(filter.Keyword ?? "")}&timeRange={filter.TimeRange ?? ""}";
        return _api.GetAsync<List<MessageDto>>(q)!.ContinueWith(t => t.Result ?? []);
    }

    public async Task<bool> DeleteMessageAsync(string messageId)
    {
        await _api.DeleteAsync($"api/chat/message/{messageId}");
        return true;
    }

    public Task<List<FriendDto>> GetFriendsAsync() =>
        _api.GetAsync<List<FriendDto>>("api/friend")!.ContinueWith(t => t.Result ?? []);

    public Task<List<FriendRequestDto>> GetPendingRequestsAsync() =>
        _api.GetAsync<List<FriendRequestDto>>("api/friend/requests/pending")!.ContinueWith(t => t.Result ?? []);

    public Task<List<UserDto>> SearchUsersAsync(string keyword) =>
        _api.GetAsync<List<UserDto>>($"api/friend/search?keyword={Uri.EscapeDataString(keyword)}")!.ContinueWith(t => t.Result ?? []);

    public async Task<(bool Success, string? Error)> SendFriendRequestAsync(string targetUserId)
    {
        try
        {
            await _api.PostAsync("api/friend/send-friend-request", new { targetUserId });
            return (true, null);
        }
        catch (ApiException ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> AcceptRequestAsync(string requestId)
    {
        try
        {
            await _api.PostAsync("api/friend/accept", new { requestId });
            return (true, null);
        }
        catch (ApiException ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> RejectRequestAsync(string requestId)
    {
        try
        {
            await _api.PostAsync("api/friend/reject", new { requestId });
            return (true, null);
        }
        catch (ApiException ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> RemoveFriendAsync(string friendId)
    {
        try
        {
            await _api.DeleteAsync($"api/friend/{friendId}");
            return (true, null);
        }
        catch (ApiException ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error, GroupDto? Group)> CreateGroupAsync(string name, List<string> memberIds)
    {
        try
        {
            var group = await _api.PostAsync<GroupDto>("api/group", new { name, memberIds });
            return (true, null, group);
        }
        catch (ApiException ex) { return (false, ex.Message, null); }
    }

    public Task<List<GroupMemberDto>> GetGroupMembersAsync(string groupId) =>
        _api.GetAsync<List<GroupMemberDto>>($"api/group/{groupId}/members")!.ContinueWith(t => t.Result ?? []);

    public Task<UserDto?> UpdateProfileAsync(string nickname, string avatarSeed) =>
        _api.PutAsync<UserDto>("api/user/profile", new { nickname, avatarSeed });

    public Task<FileUploadResult?> UploadFileAsync(Stream stream, string fileName) =>
        _api.UploadAsync(stream, fileName);
}
