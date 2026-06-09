using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;

namespace ChatApp.Admin.Services;

public class ApiSettings
{
    public const string SectionName = "Api";
    public string BaseUrl { get; set; } = "http://localhost:5200";
}

public class HttpApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly AdminAuthState _auth;

    public HttpApiClient(HttpClient http, AdminAuthState auth)
    {
        _http = http;
        _auth = auth;
    }

    public void SetBaseUrl(string baseUrl) => _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

    public async Task<T?> GetAsync<T>(string path)
    {
        using var request = CreateRequest(HttpMethod.Get, path);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task PostAsync(string path, object? body = null)
    {
        using var request = CreateRequest(HttpMethod.Post, path, body);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string path)
    {
        using var request = CreateRequest(HttpMethod.Delete, path);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<T?> PostAsync<T>(string path, object? body = null)
    {
        using var request = CreateRequest(HttpMethod.Post, path, body);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrEmpty(_auth.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return request;
    }
}

public class AdminAuthState
{
    public string? Token { get; set; }
    public string? AdminName { get; set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
    public event Action? OnChange;
    public void Notify() => OnChange?.Invoke();
}

public class HttpAdminAuthService : IAdminAuthService
{
    private readonly HttpApiClient _api;
    private readonly AdminAuthState _state;

    public HttpAdminAuthService(HttpApiClient api, AdminAuthState state)
    {
        _api = api;
        _state = state;
    }

    public bool IsLoggedIn => _state.IsLoggedIn;
    public event Action? OnChange
    {
        add => _state.OnChange += value;
        remove => _state.OnChange -= value;
    }

    public bool Login(string username, string password)
    {
        try
        {
            var response = Task.Run(() =>
                _api.PostAsync<LoginResponse>("api/admin/auth/login", new { username, password }))
                .GetAwaiter().GetResult();
            if (response?.Token is null) return false;
            _state.Token = response.Token;
            _state.AdminName = response.AdminName ?? "超级管理员";
            _state.Notify();
            return true;
        }
        catch { return false; }
    }

    public void Logout()
    {
        _state.Token = null;
        _state.AdminName = null;
        _state.Notify();
    }
}

public class HttpAdminService : IAdminService
{
    private readonly HttpApiClient _api;

    public HttpAdminService(HttpApiClient api) => _api = api;

    private static T RunSync<T>(Func<Task<T>> work) =>
        Task.Run(work).GetAwaiter().GetResult();

    private static void RunSync(Func<Task> work) =>
        Task.Run(work).GetAwaiter().GetResult();

    public int GetPendingCount()
    {
        var r = RunSync(() => _api.GetAsync<PendingRegistrationsResponse>("api/admin/registrations/pending"));
        return r?.Count ?? 0;
    }

    public List<UserDto> GetPendingRegistrations()
    {
        var r = RunSync(() => _api.GetAsync<PendingRegistrationsResponse>("api/admin/registrations/pending"));
        return r?.Items ?? [];
    }

    public List<UserDto> GetAllUsers(string? keyword, UserStatus? status)
    {
        var q = $"api/admin/users?keyword={Uri.EscapeDataString(keyword ?? "")}&status={(status.HasValue ? (int)status.Value : "")}";
        return RunSync(() => _api.GetAsync<List<UserDto>>(q)) ?? [];
    }

    public List<MessageDto> QueryMessages(MessageQueryFilter filter)
    {
        var q = $"api/admin/messages?sender={Uri.EscapeDataString(filter.SenderUsername ?? "")}&keyword={Uri.EscapeDataString(filter.Keyword ?? "")}&timeRange={filter.TimeRange}";
        return RunSync(() => _api.GetAsync<List<MessageDto>>(q)) ?? [];
    }

    public bool ApproveRegistration(string userId)
    {
        try { RunSync(() => _api.PostAsync($"api/admin/registrations/{userId}/approve")); return true; }
        catch { return false; }
    }

    public bool RejectRegistration(string userId)
    {
        try { RunSync(() => _api.PostAsync($"api/admin/registrations/{userId}/reject")); return true; }
        catch { return false; }
    }

    public bool BanUser(string userId)
    {
        try { RunSync(() => _api.PostAsync($"api/admin/users/{userId}/ban")); return true; }
        catch { return false; }
    }

    public bool UnbanUser(string userId)
    {
        try { RunSync(() => _api.PostAsync($"api/admin/users/{userId}/unban")); return true; }
        catch { return false; }
    }

    public bool DeleteMessage(string messageId)
    {
        try { RunSync(() => _api.DeleteAsync($"api/admin/messages/{messageId}")); return true; }
        catch { return false; }
    }

    public bool DeleteMessages(IEnumerable<string> messageIds)
    {
        try
        {
            var ids = messageIds.Select(id => long.TryParse(id, out var v) ? v : 0).Where(v => v > 0).ToList();
            RunSync(() => _api.PostAsync("api/admin/messages/batch-delete", new { ids }));
            return true;
        }
        catch { return false; }
    }
}
