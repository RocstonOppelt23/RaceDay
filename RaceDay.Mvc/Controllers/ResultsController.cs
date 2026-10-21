using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class ResultsController : Controller
{
    private readonly IApiClient _api;

    public ResultsController(IApiClient api)
    {
        _api = api;
    }

    [RequireSessionRole(Roles.Organiser)]
    public async Task<IActionResult> Index()
    {
        var events = await _api.GetAsync<List<EventViewModel>>("/api/events") ?? new();
        int? organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId);

        var rows = new List<OrganiserEventSummary>();
        foreach (var ev in events.Where(e => e.OrganiserId == organiserId).OrderByDescending(e => e.EventDate))
        {
            var enrolments = await _api.GetAsync<List<EnrolmentViewModel>>($"/api/events/{ev.EventId}/enrolments");
            var results = await _api.GetAsync<List<ResultViewModel>>($"/api/events/{ev.EventId}/results");
            rows.Add(new OrganiserEventSummary
            {
                Event = ev,
                EnrolmentCount = enrolments?.Count(e => e.IsActive) ?? 0,
                ResultCount = results?.Count ?? 0
            });
        }

        return View(rows);
    }

    [RequireSessionRole(Roles.Organiser)]
    public async Task<IActionResult> EventResults(int eventId)
    {
        var ev = await _api.GetAsync<EventViewModel>($"/api/events/{eventId}");
        if (ev is null) return NotFound();
        if (ev.OrganiserId != HttpContext.Session.GetInt32(SessionKeys.UserId))
            return RedirectToAction("AccessDenied", "Account");

        var results = await _api.GetAsync<List<ResultViewModel>>($"/api/events/{eventId}/results") ?? new();

        return View(new EventResultsViewModel
        {
            Event = ev,
            Results = results.OrderBy(r => r.FinishingPosition).ToList()
        });
    }

    [RequireSessionRole(Roles.Organiser)]
    [HttpGet]
    public async Task<IActionResult> Capture(int enrolmentId, int eventId)
    {
        if (enrolmentId <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var enrolments = await _api.GetAsync<List<EnrolmentViewModel>>($"/api/events/{eventId}/enrolments");
        var enrolment = enrolments?.FirstOrDefault(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound();

        var ev = await _api.GetAsync<EventViewModel>($"/api/events/{eventId}");

        return View(new ResultFormViewModel
        {
            EnrolmentId = enrolmentId,
            EventId = eventId,
            EventName = ev?.Name ?? "",
            ParticipantName = enrolment.ParticipantName,
            CategoryName = enrolment.CategoryName
        });
    }

    [RequireSessionRole(Roles.Organiser)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Capture(ResultFormViewModel model)
    {
        if (!TimeSpan.TryParseExact(model.FinishTime, @"h\:mm\:ss", CultureInfo.InvariantCulture, out var finishTime)
            && !TimeSpan.TryParseExact(model.FinishTime, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out finishTime))
        {
            ModelState.AddModelError(nameof(model.FinishTime), "Use hh:mm:ss, for example 00:52:35.");
        }
        else if (finishTime <= TimeSpan.Zero)
        {
            ModelState.AddModelError(nameof(model.FinishTime), "The finish time must be more than zero.");
        }

        if (!ModelState.IsValid) return View(model);

        var response = await _api.PostAsync($"/api/enrolments/{model.EnrolmentId}/result", new
        {
            FinishTime = finishTime,
            model.FinishingPosition
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ApiErrors.ReadMessageAsync(response,
                "Could not save the result. It may already exist for this enrolment."));
            return View(model);
        }

        TempData["Message"] = $"Result published for {model.ParticipantName}.";
        return RedirectToAction("EventEnrolments", "Enrolments", new { eventId = model.EventId });
    }

    [RequireSessionRole(Roles.Participant)]
    public async Task<IActionResult> MyResults()
    {
        var results = await _api.GetAsync<List<ResultViewModel>>("/api/results/my") ?? new();
        return View(results.OrderByDescending(r => r.EventDate).ToList());
    }
}
