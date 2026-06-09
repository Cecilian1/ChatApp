using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;
using ChatApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminUsersController(IAdminBusinessService admin) : ControllerBase
{
    [HttpGet("registrations/pending")]
    public async Task<IActionResult> GetPending() => Ok(await admin.GetPendingRegistrationsAsync());

    [HttpPost("registrations/{id}/approve")]
    public async Task<IActionResult> Approve(long id) =>
        await admin.ApproveRegistrationAsync(id) ? Ok(ApiResult.Ok()) : NotFound(ApiResult.Fail("用户不存在"));

    [HttpPost("registrations/{id}/reject")]
    public async Task<IActionResult> Reject(long id) =>
        await admin.RejectRegistrationAsync(id) ? Ok(ApiResult.Ok()) : NotFound(ApiResult.Fail("用户不存在"));

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? keyword, [FromQuery] UserStatus? status) =>
        Ok(await admin.GetUsersAsync(keyword, status));

    [HttpPost("users/{id}/ban")]
    public async Task<IActionResult> Ban(long id) =>
        await admin.BanUserAsync(id) ? Ok(ApiResult.Ok()) : NotFound(ApiResult.Fail("用户不存在"));

    [HttpPost("users/{id}/unban")]
    public async Task<IActionResult> Unban(long id) =>
        await admin.UnbanUserAsync(id) ? Ok(ApiResult.Ok()) : NotFound(ApiResult.Fail("用户不存在"));
}

[ApiController]
[Route("api/admin/messages")]
[Authorize(Roles = "admin")]
public class AdminMessagesController(IAdminBusinessService admin) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] MessageQueryFilter filter) =>
        Ok(await admin.QueryMessagesAsync(filter));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id) =>
        await admin.DeleteMessageAsync(id) ? Ok(ApiResult.Ok()) : NotFound();

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteRequest request) =>
        Ok(new { success = await admin.DeleteMessagesAsync(request.Ids) });
}

public class BatchDeleteRequest
{
    public List<long> Ids { get; set; } = [];
}
