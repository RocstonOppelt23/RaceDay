using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Participants entering events, and the checks around it.</summary>
public class EnrolmentsControllerTests
{
    [Fact]
    public void Enrol_AsParticipant_RecordsParticipantEventAndCategory()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        var result = controller.Enrol(ev.EventId, new EnrolmentDto(category.CategoryId));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);

        var saved = Assert.Single(context.Enrolments);
        Assert.Equal(participant.UserId, saved.ParticipantId);
        Assert.Equal(ev.EventId, saved.EventId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
        Assert.Equal("Active", saved.Status);
    }

    [Fact]
    public void Enrol_TwiceForTheSameEventAndCategory_ReturnsConflict()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        controller.Enrol(ev.EventId, new EnrolmentDto(category.CategoryId));
        var second = controller.Enrol(ev.EventId, new EnrolmentDto(category.CategoryId));

        Assert.IsType<ConflictObjectResult>(second);
        Assert.Single(context.Enrolments);
    }

    [Fact]
    public void Enrol_WithACategoryFromAnotherEvent_IsRejected()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, _) = TestHelper.Seed(context);

        var otherEvent = new Event
        {
            OrganiserId = organiser.UserId,
            Name = "Cape Cycle Challenge",
            EventDate = new DateTime(2027, 5, 22, 6, 30, 0),
            Location = "Cape Town",
            Distance = 42.00m,
            EventType = "Cycle"
        };
        context.Events.Add(otherEvent);
        context.SaveChanges();

        var foreignCategory = new Category { EventId = otherEvent.EventId, Name = "Open Cycle" };
        context.Categories.Add(foreignCategory);
        context.SaveChanges();

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        var result = controller.Enrol(ev.EventId, new EnrolmentDto(foreignCategory.CategoryId));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Enrolments);
    }

    [Fact]
    public void Enrol_InAnUnknownEvent_ReturnsNotFound()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, _, category) = TestHelper.Seed(context);

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        Assert.IsType<NotFoundObjectResult>(
            controller.Enrol(9999, new EnrolmentDto(category.CategoryId)));
    }

    [Fact]
    public void GetMyEnrolments_ReturnsOnlyTheLoggedInParticipantsRows()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);
        var other = TestHelper.AddUser(context, "Aisha", "aisha@raceday.co.za", "Participant");

        TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);
        TestHelper.AddEnrolment(context, other.UserId, ev.EventId, category.CategoryId);

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        var ok = Assert.IsType<OkObjectResult>(controller.GetMyEnrolments());
        var rows = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);

        Assert.Single(rows.Cast<object>());
    }

    [Fact]
    public void GetEventEnrolments_ByTheOwningOrganiser_ListsTheEntrants()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, category) = TestHelper.Seed(context);
        TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var ok = Assert.IsType<OkObjectResult>(controller.GetEventEnrolments(ev.EventId));
        var rows = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);

        Assert.Single(rows.Cast<object>());
    }

    [Fact]
    public void GetEventEnrolments_ByAnotherOrganiser_IsForbidden()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);
        TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var intruder = TestHelper.AddUser(context, "Pieter", "other@raceday.co.za", "Organiser");

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, intruder.UserId, "Organiser");

        var status = Assert.IsType<ObjectResult>(controller.GetEventEnrolments(ev.EventId));
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public void CancelEnrolment_ByTheOwner_SetsStatusToCancelled()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        var result = controller.CancelEnrolment(enrolment.EnrolmentId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Cancelled", context.Enrolments.Find(enrolment.EnrolmentId)!.Status);
    }

    [Fact]
    public void CancelEnrolment_ByAnotherParticipant_IsForbidden()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var other = TestHelper.AddUser(context, "Aisha", "aisha@raceday.co.za", "Participant");

        var controller = new EnrolmentsController(context);
        TestHelper.SignIn(controller, other.UserId, "Participant");

        var status = Assert.IsType<ObjectResult>(controller.CancelEnrolment(enrolment.EnrolmentId));
        Assert.Equal(403, status.StatusCode);
        Assert.Equal("Active", context.Enrolments.Find(enrolment.EnrolmentId)!.Status);
    }
}
