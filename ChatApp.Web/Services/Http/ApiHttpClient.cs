using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ChatApp.Shared.Models;
using Microsoft.Extensions.Options;

namespace ChatApp.Web.Services.Http;

public class ApiHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiHttpClient(HttpClient http, IHttpContextAccessor httpContextAccessor, IOptions<ApiSettings> settings)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _http.BaseAddress = new Uri(settings.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<T?> GetAsync<T>(string path)
    {
        using var request = CreateRequest(HttpMethod.Get, path);
        var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<T?> PostAsync<T>(string path, object? body = null)
    {
        using var request = CreateRequest(HttpMethod.Post, path, body);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions);
            throw new ApiException(err?.Error ?? response.ReasonPhrase ?? "请求失败");
        }
        if (response.Content.Headers.ContentLength == 0) return default;
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task PostAsync(string path, object? body = null)
    {
        using var request = CreateRequest(HttpMethod.Post, path, body);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions);
            throw new ApiException(err?.Error ?? "请求失败");
        }
    }

    public async Task DeleteAsync(string path)
    {
        using var request = CreateRequest(HttpMethod.Delete, path);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<T?> UploadAsync<T>(string path, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", file.FileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        AttachToken(request);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
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
        var token = _httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.JwtToken);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

public class ApiException(string message) : Exception(message);
