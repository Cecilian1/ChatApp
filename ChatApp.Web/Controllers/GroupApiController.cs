using ChatApp.Web.Filters;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireLogin]
public class GroupApiController : ControllerBase
{
    private readonly IUserAccountService _accountService;
    private readonly IGroupService _groupService;

    public GroupApiController(IUserAccountService accountService, IGroupService groupService)
    {
        _accountService = accountService;
        _groupService = groupService;
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
}

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = [];
}
