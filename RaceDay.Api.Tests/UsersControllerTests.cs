using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Profile access — a user may only read and write their own record.</summary>
public class UsersControllerTests
{
    [Fact]
    public void GetMyProfile_ReturnsTheLoggedInUserNotSomeoneElse()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, _, _) = TestHelper.Seed(context);

        var controller = new UsersController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        var ok = Assert.IsType<OkObjectResult>(controller.GetMyProfile());
        var value = ok.Value!;
        var userId = (int)value.GetType().GetProperty("UserId")!.GetValue(value)!;

        Assert.Equal(participant.UserId, userId);
        Assert.NotEqual(organiser.UserId, userId);
    }

    [Fact]
    public void UpdateMyProfile_SavesNameAndProfileDetails()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, _, _) = TestHelper.Seed(context);

        var controller = new UsersController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        var result = controller.UpdateMyProfile(new UpdateProfileDto(
            "Sipho", "Dlamini", "sipho.new@raceday.co.za",
            "0825551234", new DateOnly(2002, 2, 10), null));

        Assert.IsType<OkObjectResult>(result);

        var saved = context.Users.Find(participant.UserId)!;
        Assert.Equal("Dlamini", saved.LastName);
        Assert.Equal("sipho.new@raceday.co.za", saved.Email);
        Assert.Equal("0825551234", context.UserProfiles.Single(p => p.UserId == participant.UserId).PhoneNumber);
    }

    [Fact]
    public void UpdateMyProfile_WithAnEmailAlreadyInUse_IsRejected()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, _, _) = TestHelper.Seed(context);

        var controller = new UsersController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        // organiser@raceday.co.za already belongs to the seeded organiser.
        var result = controller.UpdateMyProfile(new UpdateProfileDto(
            "Sipho", "Dlamini", "organiser@raceday.co.za", null, null, null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void UpdateMyProfile_WithMissingName_IsRejected()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, _, _) = TestHelper.Seed(context);

        var controller = new UsersController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        var result = controller.UpdateMyProfile(new UpdateProfileDto(
            "", "Dlamini", "sipho@raceday.co.za", null, null, null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetUser_WithUnknownId_ReturnsNotFound()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new UsersController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        Assert.IsType<NotFoundObjectResult>(controller.GetUser(9999));
    }
}
