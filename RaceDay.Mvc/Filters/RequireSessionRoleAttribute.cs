using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// Protects MVC actions (not API endpoints). Reads the LOCAL MVC session set at login:
/// nobody logged in → Login page; wrong role → Access Denied page.
/// [RequireSessionRole] with no role just requires a login.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireSessionRoleAttribute : ActionFilterAttribute
{
    private readonly string? _requiredRole;

    public RequireSessionRoleAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public string? RequiredRole => _requiredRole;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = context.HttpContext.Session.GetString(SessionKeys.Role);

        if (string.IsNullOrEmpty(role))
        {
            // Send them back to where they were going once they've logged in.
            var request = context.HttpContext.Request;
            string returnUrl = request.Path + request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_requiredRole != null && role != _requiredRole)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
