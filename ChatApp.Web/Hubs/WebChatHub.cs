using ChatApp.Web.Services;
using ChatApp.Web.Services.Http;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Hubs;

public class WebChatHub(ApiRealtimeBridge bridge) : Hub
{
    public static string UserGroup(string userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userId = httpContext?.Session.GetString(SessionKeys.UserId);
        var jwt = httpContext?.Session.GetString(SessionKeys.JwtToken);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwt))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await bridge.AttachClientAsync(userId, jwt);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        var userId = httpContext?.Session.GetString(SessionKeys.UserId);
        if (!string.IsNullOrEmpty(userId))
            await bridge.DetachClientAsync(userId);

        await base.OnDisconnectedAsync(exception);
    }
}
