using ChatApp.Shared.Models;
using ChatApp.Api.Hubs;
using ChatApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Roles = "user")]
public class ChatController(IChatBusinessService chat, IHubContext<ChatHub> hubContext) : ControllerBase
{
    private long UserId => User.GetUserId();

    private static MessageDto AsBroadcast(MessageDto msg) => new()
    {
        Id = msg.Id, SessionId = msg.SessionId, SenderId = msg.SenderId,
        SenderName = msg.SenderName, SenderAvatarSeed = msg.SenderAvatarSeed,
        ReceiverName = msg.ReceiverName, Type = msg.Type, Content = msg.Content,
        FileName = msg.FileName, FileSize = msg.FileSize, FileProgress = msg.FileProgress,
        SentAt = msg.SentAt, IsMine = false
    };

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions() => Ok(await chat.GetSessionsAsync(UserId));

    [HttpGet("messages/{conversationId}")]
    public async Task<IActionResult> GetMessages(long conversationId) =>
        Ok(await chat.GetMessagesAsync(UserId, conversationId));

    [HttpGet("session/{conversationId}")]
    public async Task<IActionResult> GetSession(long conversationId)
    {
        var session = await chat.GetSessionAsync(UserId, conversationId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        if (!long.TryParse(request.SessionId, out var convId))
            return BadRequest(ApiResult.Fail("无效会话"));
        var msg = await chat.SendTextAsync(UserId, convId, request.Content);
        if (msg is null) return BadRequest(ApiResult.Fail("发送失败"));
        await ChatHub.NotifyMembersAsync(hubContext, chat, convId, UserId, AsBroadcast(msg));
        return Ok(msg);
    }

    [HttpPost("send-file")]
    public async Task<IActionResult> SendFile([FromBody] SendFileRequest request)
    {
        if (!long.TryParse(request.SessionId, out var convId))
            return BadRequest(ApiResult.Fail("无效会话"));
        var msg = await chat.SendFileAsync(UserId, convId, request.FileName, request.Content, request.FileSizeBytes);
        if (msg is null) return BadRequest(ApiResult.Fail("发送失败"));
        await ChatHub.NotifyMembersAsync(hubContext, chat, convId, UserId, AsBroadcast(msg));
        return Ok(msg);
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] MessageQueryFilter filter) =>
        Ok(await chat.QueryHistoryAsync(UserId, filter));

    [HttpDelete("message/{messageId}")]
    public async Task<IActionResult> DeleteMessage(long messageId) =>
        await chat.DeleteMessageAsync(UserId, messageId) ? Ok(ApiResult.Ok()) : NotFound();

    [HttpPost("messages/delete")]
    public async Task<IActionResult> DeleteMessages([FromBody] DeleteMessagesRequest request)
    {
        var ids = request.MessageIds.Select(id => long.TryParse(id, out var v) ? v : 0).Where(v => v > 0);
        return Ok(new { success = await chat.DeleteMessagesAsync(UserId, ids) });
    }
}

public class SendMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class SendFileRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public class DeleteMessagesRequest
{
    public List<string> MessageIds { get; set; } = [];
}
