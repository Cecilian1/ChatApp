using ChatApp.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApp.Client.Services;

public class ChatHubService : IAsyncDisposable
{
    private readonly UserAuthState _auth;
    private readonly ApiSettings _settings;
    private HubConnection? _connection;
    private string? _joinedConversationId;

    public ChatHubService(UserAuthState auth, ApiSettings settings)
    {
        _auth = auth;
        _settings = settings;
    }

    public event Action<MessageDto>? MessageReceived;

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

        await _connection.StartAsync();

        if (!string.IsNullOrEmpty(_joinedConversationId))
            await JoinConversationAsync(_joinedConversationId);
    }

    public async Task DisconnectAsync()
    {
        if (_connection is null) return;
        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;
        _joinedConversationId = null;
    }

    public async Task JoinConversationAsync(string conversationId)
    {
        _joinedConversationId = conversationId;
        if (_connection?.State != HubConnectionState.Connected) return;
        await _connection.InvokeAsync("JoinConversation", conversationId);
    }

    public async Task LeaveConversationAsync(string conversationId)
    {
        if (_connection?.State != HubConnectionState.Connected) return;
        await _connection.InvokeAsync("LeaveConversation", conversationId);
        if (_joinedConversationId == conversationId)
            _joinedConversationId = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
