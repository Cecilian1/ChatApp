using ChatApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

[Authorize]
public class ChatHub(IChatBusinessService chat, IHubContext<ChatHub> hubContext) : Hub
{
    public async Task JoinConversation(string conversationId)
    {
        if (!long.TryParse(conversationId, out var convId)) return;
        var userId = Context.User!.GetUserId();
        if (!await chat.IsMemberAsync(userId, convId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(convId));
    }

    public async Task LeaveConversation(string conversationId)
    {
        if (!long.TryParse(conversationId, out var convId)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(convId));
    }

    public async Task SendMessage(string conversationId, string content)
    {
        if (!long.TryParse(conversationId, out var convId)) return;
        var userId = Context.User!.GetUserId();
        var msg = await chat.SendTextAsync(userId, convId, content);
        if (msg is null) return;
        await hubContext.Clients.Group(GroupName(convId)).SendAsync("ReceiveMessage", msg);
    }

    public async Task SendFileMessage(string conversationId, string fileName, string filePath, long fileSize)
    {
        if (!long.TryParse(conversationId, out var convId)) return;
        var userId = Context.User!.GetUserId();
        var msg = await chat.SendFileAsync(userId, convId, fileName, filePath, fileSize);
        if (msg is null) return;
        await hubContext.Clients.Group(GroupName(convId)).SendAsync("ReceiveMessage", msg);
    }

    public static string GroupName(long conversationId) => $"conv-{conversationId}";
}
