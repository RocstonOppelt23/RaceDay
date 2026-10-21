using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;

namespace RaceDay.Mvc.Filters;

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
