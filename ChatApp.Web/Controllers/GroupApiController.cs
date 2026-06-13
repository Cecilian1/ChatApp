using ChatApp.Shared.Models;
using ChatApp.Web.Filters;
using ChatApp.Web.Services;
using ChatApp.Web.Services.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

[Route("api/Group")]
[ApiController]
[RequireLogin]
public class GroupApiController : ControllerBase
{
    private readonly IUserAccountService _accountService;
    private readonly IGroupService _groupService;
    private readonly ApiHttpClient _api;

    public GroupApiController(IUserAccountService accountService, IGroupService groupService, ApiHttpClient api)
    {
        _accountService = accountService;
        _groupService = groupService;
        _api = api;
    }

    [HttpGet]
    public IActionResult GetGroups()
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        return Ok(_groupService.GetGroups(userId));
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateGroupRequest request)
    {
        var userId = _accountService.GetCurrentUserId(HttpContext)!;
        var (success, error, group) = _groupService.CreateGroup(userId, request.Name, request.MemberIds);
        return success ? Ok(group) : BadRequest(new { success = false, error });
    }

    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetMembers(string groupId)
    {
        var members = await _api.GetAsync<List<GroupMemberDto>>($"api/group/{groupId}/members");
        return Ok(members ?? []);
    }
}

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = [];
}
