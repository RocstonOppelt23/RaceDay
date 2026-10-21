using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

/// <summary>The Organiser's landing page.</summary>
[RequireSessionRole(Roles.Organiser)]
public class DashboardController : Controller
{
    private readonly IApiClient _api;

    public DashboardController(IApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _api.GetAsync<List<EventViewModel>>("/api/events") ?? new();
        int? organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId);

        var myEvents = events.Where(e => e.OrganiserId == organiserId).ToList();

        var summary = new DashboardViewModel
        {
            OrganiserName = HttpContext.Session.GetString(SessionKeys.FullName) ?? "Organiser",
            TotalEvents = myEvents.Count,
            UpcomingCount = myEvents.Count(e => e.IsUpcoming),
            PastCount = myEvents.Count(e => !e.IsUpcoming),
            UpcomingEvents = myEvents
                .Where(e => e.IsUpcoming)
                .OrderBy(e => e.EventDate)
                .ToList()
        };

        foreach (var ev in myEvents)
        {
            var enrolments = await _api.GetAsync<List<EnrolmentViewModel>>(
                $"/api/events/{ev.EventId}/enrolments");
            summary.EnrolmentCounts[ev.EventId] = enrolments?.Count(e => e.IsActive) ?? 0;
        }

        summary.TotalEnrolments = summary.EnrolmentCounts.Values.Sum();

        return View(summary);
    }
}
