using ChatApp.Web.Filters;
using ChatApp.Web.Services;
using ChatApp.Web.Services.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatApp.Web.Controllers;

[RequireLogin]
public class ChatController : Controller
{
    private readonly IUserAccountService _accountService;
    private readonly IChatService _chatService;
    private readonly IFriendService _friendService;
    private readonly ApiSettings _apiSettings;

    public ChatController(
        IUserAccountService accountService,
        IChatService chatService,
        IFriendService friendService,
        IOptions<ApiSettings> apiSettings)
    {
        _accountService = accountService;
        _chatService = chatService;
        _friendService = friendService;
        _apiSettings = apiSettings.Value;
    }

    public IActionResult Index()
    {
        var user = _accountService.GetCurrentUser(HttpContext)!;
        var sessions = _chatService.GetSessions(user.Id);
        var friends = _friendService.GetFriends(user.Id);
        var pendingRequests = _friendService.GetPendingRequests(user.Id);

        ViewBag.CurrentUser = user;
        ViewBag.Sessions = sessions;
        ViewBag.Friends = friends;
        ViewBag.PendingRequests = pendingRequests;
        ViewBag.ActiveSessionId = sessions.FirstOrDefault()?.Id;
        ViewBag.ApiBaseUrl = _apiSettings.BaseUrl;
        ViewBag.JwtToken = HttpContext.Session.GetString(SessionKeys.JwtToken) ?? "";

        if (sessions.Count > 0)
        {
            ViewBag.Messages = _chatService.GetMessages(user.Id, sessions[0].Id);
            ViewBag.ActiveSession = sessions[0];
        }

        return View();
    }
}
