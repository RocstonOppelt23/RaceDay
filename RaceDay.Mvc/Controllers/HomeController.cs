using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IApiClient _api;

    public HomeController(IApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? type, DateTime? fromDate, string? search)
    {
        var events = await _api.GetAsync<List<EventViewModel>>("/api/events/upcoming") ?? new();
        IEnumerable<EventViewModel> filtered = events;

        if (!string.IsNullOrEmpty(type))
            filtered = filtered.Where(e => e.EventType == type);

        if (fromDate.HasValue)
            filtered = filtered.Where(e => e.EventDate.Date >= fromDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));

        return View(new BrowseEventsViewModel
        {
            Events = filtered.OrderBy(e => e.EventDate).ToList(),
            Type = type,
            FromDate = fromDate,
            Search = search
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }

    [ActionName("StatusCode")]
    public IActionResult HttpStatus(int code)
    {
        ViewBag.Code = code;
        return View("StatusCode");
    }
}
