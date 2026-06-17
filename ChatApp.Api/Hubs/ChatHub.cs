using ChatApp.Api.Services;
using ChatApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

[Authorize]
public class ChatHub(IChatBusinessService chat, IHubContext<ChatHub> hubContext) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User!.GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User!.GetUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroupName(userId));
        await base.OnDisconnectedAsync(exception);
    }

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
        await ChatHub.NotifyMembersAsync(hubContext, chat, convId, userId, AsBroadcast(msg));
    }

    public async Task SendFileMessage(string conversationId, string fileName, string filePath, long fileSize)
    {
        if (!long.TryParse(conversationId, out var convId)) return;
        var userId = Context.User!.GetUserId();
        var msg = await chat.SendFileAsync(userId, convId, fileName, filePath, fileSize);
        if (msg is null) return;
        await ChatHub.NotifyMembersAsync(hubContext, chat, convId, userId, AsBroadcast(msg));
    }

    private static MessageDto AsBroadcast(MessageDto msg) => new()
    {
        Id = msg.Id, SessionId = msg.SessionId, SenderId = msg.SenderId,
        SenderName = msg.SenderName, SenderAvatarSeed = msg.SenderAvatarSeed,
        ReceiverName = msg.ReceiverName, Type = msg.Type, Content = msg.Content,
        FileName = msg.FileName, FileSize = msg.FileSize, FileProgress = msg.FileProgress,
        SentAt = msg.SentAt, IsMine = false
    };

    public static string GroupName(long conversationId) => $"conv-{conversationId}";
    public static string UserGroupName(long userId) => $"user-{userId}";

    public static async Task NotifyMembersAsync(
        IHubContext<ChatHub> hubContext,
        IChatBusinessService chat,
        long conversationId,
        long senderId,
        MessageDto message)
    {
        var memberIds = await chat.GetMemberUserIdsAsync(conversationId);
        foreach (var memberId in memberIds)
        {
            if (memberId == senderId) continue;
            await hubContext.Clients.Group(UserGroupName(memberId)).SendAsync("ReceiveMessage", message);
        }
    }
}
