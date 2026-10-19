using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Registration and login, success and failure paths.</summary>
public class AuthControllerTests
{
    [Fact]
    public void Register_AsOrganiser_StoresUserAndHashesPassword()
    {
        using var context = TestHelper.NewContext();
        var controller = new AuthController(context);

        var result = controller.Register(
            new RegisterDto("Rocston", "Oppelt", "rocky@raceday.co.za", "Pass123!", "Organiser"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);

        var saved = Assert.Single(context.Users);
        Assert.Equal("rocky@raceday.co.za", saved.Email);
        Assert.Equal("Organiser", saved.Role);

        // The plain password must never reach the database.
        Assert.NotEqual("Pass123!", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Pass123!", saved.PasswordHash));
    }

    [Fact]
    public void Register_AsParticipant_CreatesAnEmptyProfileRow()
    {
        using var context = TestHelper.NewContext();
        var controller = new AuthController(context);

        controller.Register(
            new RegisterDto("Sipho", "Dlamini", "sipho@raceday.co.za", "Pass123!", "Participant"));

        Assert.Single(context.UserProfiles);
    }

    [Fact]
    public void Register_WithInvalidRole_IsRejected()
    {
        using var context = TestHelper.NewContext();
        var controller = new AuthController(context);

        var result = controller.Register(
            new RegisterDto("Admin", "Person", "admin@raceday.co.za", "Pass123!", "Administrator"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Users);
    }

    [Fact]
    public void Register_WithDuplicateEmail_ReturnsConflict()
    {
        using var context = TestHelper.NewContext();
        TestHelper.Seed(context);
        var controller = new AuthController(context);

        var result = controller.Register(
            new RegisterDto("Copy", "Cat", "organiser@raceday.co.za", "Pass123!", "Participant"));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(2, context.Users.Count());
    }

    [Fact]
    public void Login_WithCorrectPassword_StoresUserIdAndRoleInSession()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new AuthController(context);
        TestHelper.SignedOut(controller);

        var result = controller.Login(new LoginDto("organiser@raceday.co.za", "Pass123!"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(organiser.UserId, controller.HttpContext.Session.GetInt32("UserId"));
        Assert.Equal("Organiser", controller.HttpContext.Session.GetString("Role"));
    }

    [Fact]
    public void Login_WithWrongPassword_ReturnsUnauthorisedAndStartsNoSession()
    {
        using var context = TestHelper.NewContext();
        TestHelper.Seed(context);

        var controller = new AuthController(context);
        TestHelper.SignedOut(controller);

        var result = controller.Login(new LoginDto("organiser@raceday.co.za", "WrongPassword"));

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetInt32("UserId"));
    }

    [Fact]
    public void Login_WithUnknownEmail_ReturnsUnauthorised()
    {
        using var context = TestHelper.NewContext();
        var controller = new AuthController(context);
        TestHelper.SignedOut(controller);

        var result = controller.Login(new LoginDto("nobody@raceday.co.za", "Pass123!"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Logout_ClearsTheSession()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new AuthController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        controller.Logout();

        Assert.Null(controller.HttpContext.Session.GetInt32("UserId"));
    }
}
