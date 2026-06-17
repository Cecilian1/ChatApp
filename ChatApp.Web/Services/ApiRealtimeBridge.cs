using System.Collections.Concurrent;
using ChatApp.Shared.Models;
using ChatApp.Web.Hubs;
using ChatApp.Web.Services.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace ChatApp.Web.Services;

public sealed class ApiRealtimeBridge : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, UserListener> _listeners = new();
    private readonly IHubContext<WebChatHub> _webHub;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<ApiRealtimeBridge> _logger;

    public ApiRealtimeBridge(
        IHubContext<WebChatHub> webHub,
        IOptions<ApiSettings> apiSettings,
        ILogger<ApiRealtimeBridge> logger)
    {
        _webHub = webHub;
        _apiSettings = apiSettings.Value;
        _logger = logger;
    }

    public async Task AttachClientAsync(string userId, string jwt)
    {
        var listener = _listeners.GetOrAdd(userId, _ => new UserListener());
        listener.AddRef();
        try
        {
            await listener.EnsureConnectedAsync(_apiSettings.BaseUrl, jwt, async (eventName, payload) =>
            {
                await _webHub.Clients.Group(WebChatHub.UserGroup(userId)).SendAsync(eventName, payload);
            });
        }
        catch (Exception ex)
        {
            listener.RemoveRef();
            if (listener.RefCount == 0)
                _listeners.TryRemove(userId, out _);
            _logger.LogWarning(ex, "Failed to connect API realtime bridge for user {UserId}", userId);
            throw;
        }
    }

    public async Task DetachClientAsync(string userId)
    {
        if (!_listeners.TryGetValue(userId, out var listener)) return;
        if (listener.RemoveRef() > 0) return;

        await listener.DisposeAsync();
        _listeners.TryRemove(userId, out _);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var listener in _listeners.Values)
            await listener.DisposeAsync();
        _listeners.Clear();
    }

    private sealed class UserListener : IAsyncDisposable
    {
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private HubConnection? _connection;
        private int _refs;
        private Func<string, object, Task>? _forwardAsync;

        public int RefCount => _refs;

        public int AddRef() => Interlocked.Increment(ref _refs);

        public int RemoveRef() => Interlocked.Decrement(ref _refs);

        public async Task EnsureConnectedAsync(string apiBase, string jwt, Func<string, object, Task> forwardAsync)
        {
            _forwardAsync = forwardAsync;
            await _connectLock.WaitAsync();
            try
            {
                if (_connection?.State == HubConnectionState.Connected) return;

                if (_connection is not null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                var hubUrl = apiBase.TrimEnd('/') + "/hubs/chat";
                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options => options.AccessTokenProvider = () => Task.FromResult<string?>(jwt))
                    .WithAutomaticReconnect()
                    .Build();

                _connection.On<MessageDto>("ReceiveMessage", msg => _forwardAsync!("ReceiveMessage", msg));
                _connection.On<FriendRequestDto>("ReceiveFriendRequest", req => _forwardAsync!("ReceiveFriendRequest", req));
                await _connection.StartAsync();
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _connectLock.WaitAsync();
            try
            {
                if (_connection is null) return;
                await _connection.DisposeAsync();
                _connection = null;
            }
            finally
            {
                _connectLock.Release();
                _connectLock.Dispose();
            }
        }
    }
}
