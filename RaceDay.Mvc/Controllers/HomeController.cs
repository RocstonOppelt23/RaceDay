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

    /// <summary>Public home page: upcoming events, no login needed.</summary>
    public async Task<IActionResult> Index()
    {
        var events = await _api.GetAsync<List<EventViewModel>>("/api/events/upcoming") ?? new();
        return View(events.Take(6).ToList());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }

    /// <summary>Friendly page for 404s and other status codes.</summary>
    [ActionName("StatusCode")]
    public IActionResult HttpStatus(int code)
    {
        ViewBag.Code = code;
        return View("StatusCode");
    }
}
