using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;
using Xunit;

namespace RaceDay.Mvc.Tests;

public class RequireSessionRoleTests
{
    private static ActionExecutingContext ContextFor(string? role, string path = "/Dashboard")
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        httpContext.Request.Path = path;
        if (role is not null) httpContext.Session.SetString(SessionKeys.Role, role);

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
            new Dictionary<string, object?>(), controller: new object());
    }

    [Fact]
    public void NotLoggedIn_RedirectsToLogin_WithReturnUrl()
    {
        var context = ContextFor(null, "/Dashboard");

        new RequireSessionRoleAttribute(Roles.Organiser).OnActionExecuting(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
        Assert.Equal("/Dashboard", redirect.RouteValues!["returnUrl"]);
    }

    [Fact]
    public void ParticipantOnOrganiserPage_IsSentToAccessDenied()
    {
        var context = ContextFor(Roles.Participant);

        new RequireSessionRoleAttribute(Roles.Organiser).OnActionExecuting(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("AccessDenied", redirect.ActionName);
    }

    [Fact]
    public void OrganiserOnParticipantPage_IsSentToAccessDenied()
    {
        var context = ContextFor(Roles.Organiser);

        new RequireSessionRoleAttribute(Roles.Participant).OnActionExecuting(context);

        Assert.Equal("AccessDenied", Assert.IsType<RedirectToActionResult>(context.Result).ActionName);
    }

    [Fact]
    public void MatchingRole_IsAllowedThrough()
    {
        var context = ContextFor(Roles.Organiser);

        new RequireSessionRoleAttribute(Roles.Organiser).OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void NoRoleRequired_AnyLoggedInUserIsAllowed()
    {
        var context = ContextFor(Roles.Participant);

        new RequireSessionRoleAttribute().OnActionExecuting(context);

        Assert.Null(context.Result);
    }
}
