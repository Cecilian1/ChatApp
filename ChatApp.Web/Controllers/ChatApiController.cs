using ChatApp.Web.Filters;
using ChatApp.Web.Services;
using ChatApp.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

[Route("api/Chat")]
[ApiController]
[RequireLogin]
public class ChatApiController : ControllerBase
{
    private readonly IUserAccountService _accountService;
    private readonly IChatService _chatService;

    public ChatApiController(IUserAccountService accountService, IChatService chatService)
    {
        _accountService = accountService;
        _chatService = chatService;
    }

    [HttpGet("sessions")]
    public IActionResult GetSessions()
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        return Ok(_chatService.GetSessions(userId));
    }

    [HttpGet("messages/{sessionId}")]
    public IActionResult GetMessages(string sessionId)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        return Ok(_chatService.GetMessages(userId, sessionId));
    }

    [HttpGet("session/{sessionId}")]
    public IActionResult GetSession(string sessionId)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var session = _chatService.GetSession(userId, sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("send")]
    public IActionResult SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var msg = _chatService.SendMessage(userId, request.SessionId, request.Content);
        return Ok(msg);
    }

    [HttpPost("send-file")]
    public IActionResult SendFileMessage([FromBody] SendFileRequest request)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var msg = _chatService.SendFileMessage(
            userId, request.SessionId, request.FileName, request.FileSizeBytes, request.Progress, request.Content);
        return Ok(msg);
    }

    [HttpGet("history")]
    public IActionResult QueryHistory([FromQuery] MessageQueryFilter filter)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        return Ok(_chatService.QueryHistory(userId, filter));
    }

    [HttpDelete("message/{messageId}")]
    public IActionResult DeleteMessage(string messageId)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var ok = _chatService.DeleteMessage(userId, messageId);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("messages/delete")]
    public IActionResult DeleteMessages([FromBody] DeleteMessagesRequest request)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var ok = _chatService.DeleteMessages(userId, request.MessageIds);
        return Ok(new { success = ok });
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
    public long FileSizeBytes { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Progress { get; set; } = 100;
}

public class DeleteMessagesRequest
{
    public List<string> MessageIds { get; set; } = [];
}
