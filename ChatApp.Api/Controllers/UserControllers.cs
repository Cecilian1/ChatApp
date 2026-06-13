using ChatApp.Api.Data;
using ChatApp.Api.Services;
using ChatApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "user")]
public class UserController(AppDbContext db) : ControllerBase
{
    private long UserId => User.GetUserId();

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await db.Users.FindAsync(UserId);
        if (user is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.Nickname))
            user.Nickname = request.Nickname.Trim();
        if (!string.IsNullOrWhiteSpace(request.AvatarSeed))
            user.AvatarSeed = request.AvatarSeed.Trim();
        await db.SaveChangesAsync();
        return Ok(AuthService.MapUser(user));
    }
}

public class UpdateProfileRequest
{
    public string? Nickname { get; set; }
    public string? AvatarSeed { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "user")]
public class FriendController(IFriendBusinessService friends) : ControllerBase
{
    private long UserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> GetFriends() => Ok(await friends.GetFriendsAsync(UserId));

    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPending() => Ok(await friends.GetPendingRequestsAsync(UserId));

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword) =>
        Ok(await friends.SearchUsersAsync(UserId, keyword));

    [HttpPost("send-friend-request")]
    public async Task<IActionResult> SendFriendRequest([FromBody] FriendActionRequest request)
    {
        if (!long.TryParse(request.TargetUserId, out var targetId))
            return BadRequest(ApiResult.Fail("无效用户"));
        var (success, error) = await friends.SendRequestAsync(UserId, targetId);
        return success ? Ok(ApiResult.Ok()) : BadRequest(ApiResult.Fail(error!));
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] RequestActionRequest request)
    {
        if (!long.TryParse(request.RequestId, out var reqId))
            return BadRequest(ApiResult.Fail("无效申请"));
        var (success, error) = await friends.AcceptRequestAsync(UserId, reqId);
        return success ? Ok(ApiResult.Ok()) : BadRequest(ApiResult.Fail(error!));
    }

    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] RequestActionRequest request)
    {
        if (!long.TryParse(request.RequestId, out var reqId))
            return BadRequest(ApiResult.Fail("无效申请"));
        var (success, error) = await friends.RejectRequestAsync(UserId, reqId);
        return success ? Ok(ApiResult.Ok()) : BadRequest(ApiResult.Fail(error!));
    }

    [HttpDelete("{friendId}")]
    public async Task<IActionResult> Remove(long friendId)
    {
        var (success, error) = await friends.RemoveFriendAsync(UserId, friendId);
        return success ? Ok(ApiResult.Ok()) : BadRequest(ApiResult.Fail(error!));
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

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "user")]
public class GroupController(IGroupBusinessService groups) : ControllerBase
{
    private long UserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> GetGroups() => Ok(await groups.GetGroupsAsync(UserId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
    {
        var memberIds = request.MemberIds.Select(id => long.TryParse(id, out var v) ? v : 0).Where(v => v > 0).ToList();
        var (success, error, group) = await groups.CreateGroupAsync(UserId, request.Name, memberIds);
        return success ? Ok(group) : BadRequest(ApiResult.Fail(error!));
    }

    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetMembers(long groupId) =>
        Ok(await groups.GetMembersAsync(UserId, groupId));
}

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = [];
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "user")]
public class FileController(IFileStorageService files) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var (success, error, fileName, url, size) = await files.SaveAsync(file);
        if (!success) return BadRequest(new { success = false, error });
        var sizeStr = size < 1024 * 1024 ? $"{size / 1024.0:F1} KB" : $"{size / 1024.0 / 1024.0:F1} MB";
        return Ok(new { success = true, fileName, fileSize = sizeStr, url, sizeBytes = size });
    }
}
