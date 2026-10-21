using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

/// <summary>
/// Participant: enter an event, see and cancel their entries.
/// Organiser: see who has entered their events.
/// </summary>
public class EnrolmentsController : Controller
{
    private readonly IApiClient _api;

    public EnrolmentsController(IApiClient api)
    {
        _api = api;
    }

    // ---------- Participant ----------

    [RequireSessionRole(Roles.Participant)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Choose a category to enter.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var response = await _api.PostAsync($"/api/events/{eventId}/enrolments", new { CategoryId = categoryId });

        if (!response.IsSuccessStatusCode)
        {
            // The API explains why: already entered (409), wrong category (400), etc.
            TempData["Error"] = await ApiErrors.ReadMessageAsync(response,
                "You are already enrolled, or the event/category is invalid.");
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        TempData["Message"] = "Enrolment successful! You're in.";
        return RedirectToAction(nameof(MyEnrolments));
    }

    [RequireSessionRole(Roles.Participant)]
    public async Task<IActionResult> MyEnrolments()
    {
        var enrolments = await _api.GetAsync<List<EnrolmentViewModel>>("/api/enrolments/my") ?? new();
        return View(enrolments
            .OrderByDescending(e => e.IsActive)
            .ThenBy(e => e.EventDate)
            .ToList());
    }

    [RequireSessionRole(Roles.Participant)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var response = await _api.DeleteAsync($"/api/enrolments/{id}");

        if (response.IsSuccessStatusCode)
            TempData["Message"] = "Your enrolment has been cancelled.";
        else
            TempData["Error"] = await ApiErrors.ReadMessageAsync(response, "The enrolment could not be cancelled.");

        return RedirectToAction(nameof(MyEnrolments));
    }

    // ---------- Organiser ----------

    /// <summary>All of the organiser's events with their entrant counts.</summary>
    [RequireSessionRole(Roles.Organiser)]
    public async Task<IActionResult> Index()
    {
        var events = await _api.GetAsync<List<EventViewModel>>("/api/events") ?? new();
        int? organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId);

        var rows = new List<OrganiserEventSummary>();
        foreach (var ev in events.Where(e => e.OrganiserId == organiserId).OrderBy(e => e.EventDate))
        {
            var enrolments = await _api.GetAsync<List<EnrolmentViewModel>>($"/api/events/{ev.EventId}/enrolments");
            rows.Add(new OrganiserEventSummary
            {
                Event = ev,
                EnrolmentCount = enrolments?.Count(e => e.IsActive) ?? 0
            });
        }

        return View(rows);
    }

    /// <summary>Everyone entered for one event, with their category, status and any result.</summary>
    [RequireSessionRole(Roles.Organiser)]
    public async Task<IActionResult> EventEnrolments(int eventId)
    {
        var ev = await _api.GetAsync<EventViewModel>($"/api/events/{eventId}");
        if (ev is null) return NotFound();
        if (ev.OrganiserId != HttpContext.Session.GetInt32(SessionKeys.UserId))
            return RedirectToAction("AccessDenied", "Account");

        var enrolments = await _api.GetAsync<List<EnrolmentViewModel>>($"/api/events/{eventId}/enrolments") ?? new();
        var results = await _api.GetAsync<List<ResultViewModel>>($"/api/events/{eventId}/results") ?? new();

        return View(new EventEnrolmentsViewModel
        {
            Event = ev,
            Enrolments = enrolments
                .OrderByDescending(e => e.IsActive)
                .ThenBy(e => e.CategoryName)
                .ThenBy(e => e.ParticipantName)
                .ToList(),
            Results = results.ToDictionary(r => r.EnrolmentId)
        });
    }
}
