using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Filters;

/// <summary>
/// Blocks the action unless the session holds the required role.
/// Use as [RequireRole("Organiser")] or [RequireRole("Participant")].
/// </summary>
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _requiredRole;

    public RequireRoleAttribute(string requiredRole) => _requiredRole = requiredRole;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        string? role = context.HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(role))
        {
            context.Result = new UnauthorizedObjectResult("You must be logged in.");
            return;
        }

        if (role != _requiredRole)
        {
            context.Result = new ObjectResult("Access denied for your role.") { StatusCode = 403 };
        }
    }
}

/// <summary>
/// Requires a logged-in user of any role.
/// </summary>
public class RequireLoginAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Session.GetInt32("UserId") is null)
        {
            context.Result = new UnauthorizedObjectResult("You must be logged in.");
        }
    }
}
