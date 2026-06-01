using ChatApp.Web.Filters;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireLogin]
public class FriendApiController : ControllerBase
{
    private readonly IUserAccountService _accountService;
    private readonly IFriendService _friendService;

    public FriendApiController(IUserAccountService accountService, IFriendService friendService)
    {
        _accountService = accountService;
        _friendService = friendService;
    }

    [HttpGet]
    public IActionResult GetFriends()
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        return Ok(_friendService.GetFriends(userId));
    }

    [HttpGet("requests/pending")]
    public IActionResult GetPendingRequests()
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        return Ok(_friendService.GetPendingRequests(userId));
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string keyword)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        return Ok(_friendService.SearchUsers(userId, keyword));
    }

    [HttpPost("request")]
    public IActionResult SendRequest([FromBody] FriendActionRequest request)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var (success, error) = _friendService.SendRequest(userId, request.TargetUserId);
        return success ? Ok(new { success = true }) : BadRequest(new { success = false, error });
    }

    [HttpPost("accept")]
    public IActionResult Accept([FromBody] RequestActionRequest request)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var (success, error) = _friendService.AcceptRequest(userId, request.RequestId);
        return success ? Ok(new { success = true }) : BadRequest(new { success = false, error });
    }

    [HttpPost("reject")]
    public IActionResult Reject([FromBody] RequestActionRequest request)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var (success, error) = _friendService.RejectRequest(userId, request.RequestId);
        return success ? Ok(new { success = true }) : BadRequest(new { success = false, error });
    }

    [HttpDelete("{friendId}")]
    public IActionResult Remove(string friendId)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var (success, error) = _friendService.RemoveFriend(userId, friendId);
        return success ? Ok(new { success = true }) : BadRequest(new { success = false, error });
    }
}

public class FriendActionRequest
{
    public string TargetUserId { get; set; } = string.Empty;
}

public class RequestActionRequest
{
    public string RequestId { get; set; } = string.Empty;
}
