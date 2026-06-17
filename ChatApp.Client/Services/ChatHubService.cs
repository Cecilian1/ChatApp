using ChatApp.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApp.Client.Services;

public class ChatHubService : IAsyncDisposable
{
    private readonly UserAuthState _auth;
    private readonly ApiSettings _settings;
    private HubConnection? _connection;
    private readonly HashSet<string> _joinedConversationIds = [];

    public ChatHubService(UserAuthState auth, ApiSettings settings)
    {
        _auth = auth;
        _settings = settings;
    }

    public event Action<MessageDto>? MessageReceived;
    public event Action<FriendRequestDto>? FriendRequestReceived;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync()
    {
        if (_connection is not null)
        {
            if (_connection.State == HubConnectionState.Connected) return;
            await _connection.DisposeAsync();
            _connection = null;
        }

        var hubUrl = _settings.BaseUrl.TrimEnd('/') + "/hubs/chat";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_auth.Token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MessageDto>("ReceiveMessage", msg => MessageReceived?.Invoke(msg));
        _connection.On<FriendRequestDto>("ReceiveFriendRequest", req => FriendRequestReceived?.Invoke(req));
        _connection.Reconnected += async _ => await RejoinConversationsAsync();

        await _connection.StartAsync();
        await RejoinConversationsAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection is null) return;
        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;
        _joinedConversationIds.Clear();
    }

    public async Task JoinConversationAsync(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId) || _joinedConversationIds.Contains(conversationId))
            return;

        _joinedConversationIds.Add(conversationId);
        if (_connection?.State != HubConnectionState.Connected) return;
        await _connection.InvokeAsync("JoinConversation", conversationId);
    }

    public async Task JoinAllConversationsAsync(IEnumerable<string> conversationIds)
    {
        foreach (var conversationId in conversationIds)
            await JoinConversationAsync(conversationId);
    }

    public async Task LeaveConversationAsync(string conversationId)
    {
        _joinedConversationIds.Remove(conversationId);
        if (_connection?.State != HubConnectionState.Connected) return;
        await _connection.InvokeAsync("LeaveConversation", conversationId);
    }

    private async Task RejoinConversationsAsync()
    {
        if (_connection?.State != HubConnectionState.Connected) return;
        foreach (var conversationId in _joinedConversationIds)
            await _connection.InvokeAsync("JoinConversation", conversationId);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
