using ChatApp.Web.Filters;
using ChatApp.Web.Services;
using ChatApp.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUserAccountService _accountService;

    public AccountController(IUserAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (_accountService.GetCurrentUserId(HttpContext) is not null)
            return RedirectToAction("Index", "Chat");
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        var (success, error, user) = _accountService.Login(username, password);
        if (!success)
        {
            ViewBag.Error = error;
            return View();
        }

        HttpContext.Session.SetString(MockUserAccountService.SessionUserIdKey, user!.Id);
        return RedirectToAction("Index", "Chat");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(RegisterRequestDto model)
    {
        var (success, error) = _accountService.Register(model);
        if (!success)
        {
            ViewBag.Error = error;
            return View(model);
        }

        ViewBag.Success = "注册申请已提交，请等待管理员审核通过后再登录。";
        return View();
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
