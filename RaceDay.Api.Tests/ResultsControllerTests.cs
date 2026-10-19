using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Capturing and viewing results.</summary>
public class ResultsControllerTests
{
    [Fact]
    public void CaptureResult_ByTheOwningOrganiser_StoresTheResult()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var result = controller.CaptureResult(
            enrolment.EnrolmentId, new ResultDto(new TimeSpan(0, 52, 35), 12));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);

        var saved = Assert.Single(context.Results);
        Assert.Equal(new TimeSpan(0, 52, 35), saved.FinishTime);
        Assert.Equal(12, saved.FinishingPosition);
    }

    [Fact]
    public void CaptureResult_TwiceForTheSameEnrolment_IsRejected()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        controller.CaptureResult(enrolment.EnrolmentId, new ResultDto(new TimeSpan(0, 52, 35), 12));
        var second = controller.CaptureResult(enrolment.EnrolmentId, new ResultDto(new TimeSpan(0, 40, 0), 1));

        Assert.IsType<BadRequestObjectResult>(second);
        Assert.Single(context.Results);
    }

    [Fact]
    public void CaptureResult_WithPositionZero_IsRejected()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var result = controller.CaptureResult(
            enrolment.EnrolmentId, new ResultDto(new TimeSpan(0, 52, 35), 0));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Results);
    }

    [Fact]
    public void CaptureResult_ByAnotherOrganiser_IsForbidden()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var intruder = TestHelper.AddUser(context, "Pieter", "other@raceday.co.za", "Organiser");

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, intruder.UserId, "Organiser");

        var status = Assert.IsType<ObjectResult>(controller.CaptureResult(
            enrolment.EnrolmentId, new ResultDto(new TimeSpan(0, 52, 35), 12)));

        Assert.Equal(403, status.StatusCode);
        Assert.Empty(context.Results);
    }

    [Fact]
    public void CaptureResult_ForAnUnknownEnrolment_ReturnsNotFound()
    {
        using var context = TestHelper.NewContext();
        var (organiser, _, _, _) = TestHelper.Seed(context);

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        Assert.IsType<NotFoundObjectResult>(
            controller.CaptureResult(9999, new ResultDto(new TimeSpan(1, 0, 0), 1)));
    }

    [Fact]
    public void GetMyResults_ReturnsOnlyTheLoggedInParticipantsResults()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);
        var other = TestHelper.AddUser(context, "Aisha", "aisha@raceday.co.za", "Participant");

        var mine = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);
        var theirs = TestHelper.AddEnrolment(context, other.UserId, ev.EventId, category.CategoryId);

        context.Results.AddRange(
            new Result { EnrolmentId = mine.EnrolmentId, FinishTime = new TimeSpan(0, 52, 35), FinishingPosition = 12 },
            new Result { EnrolmentId = theirs.EnrolmentId, FinishTime = new TimeSpan(0, 55, 18), FinishingPosition = 18 });
        context.SaveChanges();

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, participant.UserId, "Participant");

        var ok = Assert.IsType<OkObjectResult>(controller.GetMyResults());
        var rows = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);

        Assert.Single(rows.Cast<object>());
    }

    [Fact]
    public void GetEventResults_ByTheOwningOrganiser_ListsEveryResult()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        context.Results.Add(new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = new TimeSpan(0, 52, 35),
            FinishingPosition = 12
        });
        context.SaveChanges();

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var ok = Assert.IsType<OkObjectResult>(controller.GetEventResults(ev.EventId));
        var rows = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);

        Assert.Single(rows.Cast<object>());
    }

    [Fact]
    public void GetResult_ByAnotherParticipant_IsForbidden()
    {
        using var context = TestHelper.NewContext();
        var (_, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = new TimeSpan(0, 52, 35),
            FinishingPosition = 12
        };
        context.Results.Add(result);
        context.SaveChanges();

        var other = TestHelper.AddUser(context, "Aisha", "aisha@raceday.co.za", "Participant");

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, other.UserId, "Participant");

        var status = Assert.IsType<ObjectResult>(controller.GetResult(result.ResultId));
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public void UpdateResult_ByTheOwningOrganiser_SavesChanges()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = new TimeSpan(0, 52, 35),
            FinishingPosition = 12
        };
        context.Results.Add(result);
        context.SaveChanges();

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        var updated = controller.UpdateResult(result.ResultId, new ResultDto(new TimeSpan(0, 51, 0), 10));

        Assert.IsType<OkObjectResult>(updated);
        Assert.Equal(10, context.Results.Find(result.ResultId)!.FinishingPosition);
    }

    [Fact]
    public void DeleteResult_ByTheOwningOrganiser_RemovesIt()
    {
        using var context = TestHelper.NewContext();
        var (organiser, participant, ev, category) = TestHelper.Seed(context);
        var enrolment = TestHelper.AddEnrolment(context, participant.UserId, ev.EventId, category.CategoryId);

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = new TimeSpan(0, 52, 35),
            FinishingPosition = 12
        };
        context.Results.Add(result);
        context.SaveChanges();

        var controller = new ResultsController(context);
        TestHelper.SignIn(controller, organiser.UserId, "Organiser");

        Assert.IsType<NoContentResult>(controller.DeleteResult(result.ResultId));
        Assert.Empty(context.Results);
    }
}
