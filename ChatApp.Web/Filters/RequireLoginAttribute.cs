using ChatApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatApp.Web.Filters;

public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var httpContext = context.HttpContext;
        var accountService = httpContext.RequestServices.GetRequiredService<IUserAccountService>();
        var userId = accountService.GetCurrentUserId(httpContext);

        if (string.IsNullOrEmpty(userId))
        {
            if (IsApiRequest(context))
            {
                context.Result = new JsonResult(new { success = false, error = "未登录" }) { StatusCode = 401 };
            }
            else
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }

    private static bool IsApiRequest(ActionExecutingContext context)
    {
        var path = context.HttpContext.Request.Path.Value ?? "";
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }
}
