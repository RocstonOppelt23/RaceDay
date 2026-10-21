using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

/// <summary>
/// Participant: browse and view events.
/// Organiser: list, create, edit and delete their own events (with banner upload).
/// </summary>
public class EventsController : Controller
{
    private readonly IApiClient _api;

    private const long MaxBannerBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };

    public EventsController(IApiClient api)
    {
        _api = api;
    }

    // ---------- Participant-facing ----------

    [RequireSessionRole(Roles.Participant)]
    public async Task<IActionResult> Browse(string? type, DateTime? fromDate, string? search)
    {
        var events = await _api.GetAsync<List<EventViewModel>>("/api/events") ?? new();

        var upcoming = events.Where(e => e.IsUpcoming);

        if (!string.IsNullOrEmpty(type))
            upcoming = upcoming.Where(e => e.EventType == type);

        if (fromDate.HasValue)
            upcoming = upcoming.Where(e => e.EventDate.Date >= fromDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(search))
            upcoming = upcoming.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));

        return View(new BrowseEventsViewModel
        {
            Events = upcoming.OrderBy(e => e.EventDate).ToList(),
            Type = type,
            FromDate = fromDate,
            Search = search
        });
    }

    /// <summary>Full event details. Any logged-in user.</summary>
    [RequireSessionRole]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventViewModel>($"/api/events/{id}");
        if (ev == null) return NotFound();

        ev.Categories = await _api.GetAsync<List<CategoryViewModel>>($"/api/events/{id}/categories") ?? new();

        string? role = HttpContext.Session.GetString(SessionKeys.Role);
        int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

        var model = new EventDetailsViewModel
        {
            Event = ev,
            Role = role,
            IsOwner = role == Roles.Organiser && ev.OrganiserId == userId
        };

        if (role == Roles.Participant)
        {
            var mine = await _api.GetAsync<List<EnrolmentViewModel>>("/api/enrolments/my") ?? new();
            model.EnteredCategoryIds = mine
                .Where(e => e.EventId == id && e.IsActive)
                .Select(e => e.CategoryId)
                .ToHashSet();
        }

        return View(model);
    }

    // ---------- Organiser-facing ----------

    [RequireSessionRole(Roles.Organiser)]
    public async Task<IActionResult> ManageIndex()
    {
        var events = await _api.GetAsync<List<EventViewModel>>("/api/events") ?? new();
        int? organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId);

        var mine = events
            .Where(e => e.OrganiserId == organiserId)
            .OrderByDescending(e => e.IsUpcoming)
            .ThenBy(e => e.EventDate)
            .ToList();

        return View(mine);
    }

    [RequireSessionRole(Roles.Organiser)]
    [HttpGet]
    public IActionResult Create() => View("Form", new EventFormViewModel());

    [RequireSessionRole(Roles.Organiser)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model, IFormFile? banner)
    {
        ValidateBanner(banner);
        if (!ModelState.IsValid) return View("Form", model);

        using var content = BuildEventForm(model, banner);
        var response = await _api.PostFileAsync("/api/events", content);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ApiErrors.ReadMessageAsync(response,
                "Could not create the event. Check the details and try again."));
            return View("Form", model);
        }

        TempData["Message"] = $"\"{model.Name}\" has been created. Add its categories next.";
        return RedirectToAction(nameof(ManageIndex));
    }

    [RequireSessionRole(Roles.Organiser)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _api.GetAsync<EventViewModel>($"/api/events/{id}");
        if (ev == null) return NotFound();
        if (ev.OrganiserId != HttpContext.Session.GetInt32(SessionKeys.UserId))
            return RedirectToAction("AccessDenied", "Account");

        var model = new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            EventType = ev.EventType,
            ExistingBannerUrl = ev.BannerImageUrl
        };

        return View("Form", model);
    }

    [RequireSessionRole(Roles.Organiser)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventFormViewModel model, IFormFile? banner)
    {
        ValidateBanner(banner);
        if (!ModelState.IsValid) return View("Form", model);

        using var content = BuildEventForm(model, banner);
        var response = await _api.PutFileAsync($"/api/events/{model.EventId}", content);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ApiErrors.ReadMessageAsync(response, "Could not update the event."));
            return View("Form", model);
        }

        TempData["Message"] = $"\"{model.Name}\" has been updated.";
        return RedirectToAction(nameof(Details), new { id = model.EventId });
    }

    /// <summary>Confirmation page before deleting.</summary>
    [RequireSessionRole(Roles.Organiser)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _api.GetAsync<EventViewModel>($"/api/events/{id}");
        if (ev == null) return NotFound();
        if (ev.OrganiserId != HttpContext.Session.GetInt32(SessionKeys.UserId))
            return RedirectToAction("AccessDenied", "Account");

        return View(ev);
    }

    [RequireSessionRole(Roles.Organiser)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var response = await _api.DeleteAsync($"/api/events/{id}");

        if (response.IsSuccessStatusCode)
            TempData["Message"] = "The event has been deleted.";
        else
            TempData["Error"] = await ApiErrors.ReadMessageAsync(response, "The event could not be deleted.");

        return RedirectToAction(nameof(ManageIndex));
    }

    // ---------- Helpers ----------

    private void ValidateBanner(IFormFile? banner)
    {
        if (banner is null || banner.Length == 0) return;

        if (banner.Length > MaxBannerBytes)
            ModelState.AddModelError("banner", "The banner image must be 5 MB or smaller.");
        else if (!AllowedImageTypes.Contains(banner.ContentType.ToLowerInvariant()))
            ModelState.AddModelError("banner", "The banner must be a JPG, PNG, WEBP or GIF image.");
    }

    /// <summary>
    /// Multipart form: the event fields and the banner image travel to the API
    /// in the same request. Field names match the API's EventDto and "bannerImage".
    /// </summary>
    private static MultipartFormDataContent BuildEventForm(EventFormViewModel model, IFormFile? banner)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(model.Name), "Name" },
            { new StringContent(model.Description ?? ""), "Description" },
            { new StringContent(model.EventDate.ToString("s", CultureInfo.InvariantCulture)), "EventDate" },
            { new StringContent(model.Location), "Location" },
            { new StringContent(model.Distance.ToString(CultureInfo.InvariantCulture)), "Distance" },
            { new StringContent(model.EventType), "EventType" }
        };

        if (banner is { Length: > 0 })
        {
            var fileContent = new StreamContent(banner.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(banner.ContentType);
            content.Add(fileContent, "BannerImage", banner.FileName);
        }

        return content;
    }
}
