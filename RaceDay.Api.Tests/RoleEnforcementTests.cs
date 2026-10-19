using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Filters;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// The authorisation filters themselves: an unauthenticated request must be
/// refused with 401, and the wrong role with 403.
/// </summary>
public class RoleEnforcementTests
{
    private static AuthorizationFilterContext ContextFor(int? userId, string? role)
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };

        if (userId is not null) httpContext.Session.SetInt32("UserId", userId.Value);
        if (role is not null) httpContext.Session.SetString("Role", role);

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void RequireRole_WhenNotLoggedIn_ReturnsUnauthorised()
    {
        var context = ContextFor(null, null);

        new RequireRoleAttribute("Organiser").OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void RequireRole_WhenRoleIsParticipant_ReturnsForbiddenForOrganiserAction()
    {
        var context = ContextFor(5, "Participant");

        new RequireRoleAttribute("Organiser").OnAuthorization(context);

        var status = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public void RequireRole_WhenRoleIsOrganiser_ReturnsForbiddenForParticipantAction()
    {
        var context = ContextFor(5, "Organiser");

        new RequireRoleAttribute("Participant").OnAuthorization(context);

        var status = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public void RequireRole_WhenRoleMatches_AllowsTheRequest()
    {
        var context = ContextFor(5, "Organiser");

        new RequireRoleAttribute("Organiser").OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void RequireLogin_WhenNotLoggedIn_ReturnsUnauthorised()
    {
        var context = ContextFor(null, null);

        new RequireLoginAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void RequireLogin_WhenLoggedIn_AllowsEitherRole()
    {
        var participant = ContextFor(5, "Participant");
        var organiser = ContextFor(6, "Organiser");

        new RequireLoginAttribute().OnAuthorization(participant);
        new RequireLoginAttribute().OnAuthorization(organiser);

        Assert.Null(participant.Result);
        Assert.Null(organiser.Result);
    }
}
