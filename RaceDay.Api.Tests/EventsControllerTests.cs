using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Event management, including ownership checks between organisers.</summary>
public class EventsControllerTests
{
    private static EventDto SampleEvent() => new(
        "Two Oceans Half",
        "21km along the peninsula.",
        new DateTime(2027, 4, 3, 6, 0, 0),
        "Newlands",
        21.10m,
        "Run");

    [Fact]
    public void CreateEvent_AsOrganiser_StoresEventAgainstTheLoggedInOrganiser()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var result = controller.CreateEvent(SampleEvent());

        Assert.IsType<CreatedAtActionResult>(result);

        var saved = context.Events.Single(e => e.Name == "Two Oceans Half");
        // The owner comes from the session, never from the request body.
        Assert.Equal(organiser.UserId, saved.OrganiserId);
    }

    [Fact]
    public void CreateEvent_WithInvalidEventType_IsRejected()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var result = controller.CreateEvent(SampleEvent() with { EventType = "Swim" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void CreateEvent_WithZeroDistance_IsRejected()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var result = controller.CreateEvent(SampleEvent() with { Distance = 0m });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void UpdateEvent_ByTheOwner_SavesChanges()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, ev, _) = TestHelper.Seed(context);

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var result = controller.UpdateEvent(ev.EventId, SampleEvent());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Two Oceans Half", context.Events.Find(ev.EventId)!.Name);
    }

    [Fact]
    public void UpdateEvent_ByAnotherOrganiser_IsForbidden()
    {
        using var context = TestHelper.NewContext();
        var (_, _, ev, _) = TestHelper.Seed(context);
        var intruder = TestHelper.AddUser(context, "Pieter", "other@raceday.co.za", "Organiser");

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, intruder.UserId, "Organiser");

        var result = controller.UpdateEvent(ev.EventId, SampleEvent());

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
        Assert.Equal("Cape Town Road Run", context.Events.Find(ev.EventId)!.Name);
    }

    [Fact]
    public void DeleteEvent_WithNoEnrolments_RemovesTheEvent()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, ev, _) = TestHelper.Seed(context);

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var result = controller.DeleteEvent(ev.EventId);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(context.Events.Find(ev.EventId));
    }

    [Fact]
    public void DeleteEvent_WithEnrolments_IsBlocked()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, category) = TestHelper.Seed(context);
        TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var result = controller.DeleteEvent(ev.EventId);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(context.Events.Find(ev.EventId));
    }

    [Fact]
    public void GetEvent_WithUnknownId_ReturnsNotFound()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        Assert.IsType<NotFoundObjectResult>(controller.GetEvent(9999));
    }

    [Fact]
    public void GetEvents_ReturnsEveryEvent()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new EventsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");
        controller.CreateEvent(SampleEvent());

        var ok = Assert.IsType<OkObjectResult>(controller.GetEvents());
        var rows = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);

        Assert.Equal(2, rows.Cast<object>().Count());
    }
}
