using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[RequireSessionRole(Roles.Organiser)]
public class CategoriesController : Controller
{
    private readonly IApiClient _api;

    public CategoriesController(IApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Manage(int eventId)
    {
        var model = await BuildModelAsync(eventId);
        if (model is null) return NotFound();
        if (model.Event.OrganiserId != HttpContext.Session.GetInt32(SessionKeys.UserId))
            return RedirectToAction("AccessDenied", "Account");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([Bind(Prefix = "NewCategory")] CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var model = await BuildModelAsync(form.EventId);
            if (model is null) return NotFound();
            model.NewCategory = form;
            return View("Manage", model);
        }

        var response = await _api.PostAsync($"/api/events/{form.EventId}/categories",
            new { form.Name, form.Description });

        if (response.IsSuccessStatusCode)
            TempData["Message"] = $"Category \"{form.Name}\" added.";
        else
            TempData["Error"] = await ApiErrors.ReadMessageAsync(response, "The category could not be added.");

        return RedirectToAction(nameof(Manage), new { eventId = form.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var response = await _api.DeleteAsync($"/api/categories/{id}");

        if (response.IsSuccessStatusCode)
            TempData["Message"] = "Category removed.";
        else
            TempData["Error"] = await ApiErrors.ReadMessageAsync(response, "The category could not be removed.");

        return RedirectToAction(nameof(Manage), new { eventId });
    }

    private async Task<CategoryManageViewModel?> BuildModelAsync(int eventId)
    {
        var ev = await _api.GetAsync<EventViewModel>($"/api/events/{eventId}");
        if (ev is null) return null;

        var categories = await _api.GetAsync<List<CategoryViewModel>>($"/api/events/{eventId}/categories") ?? new();

        return new CategoryManageViewModel
        {
            Event = ev,
            Categories = categories,
            NewCategory = new CategoryFormViewModel { EventId = eventId }
        };
    }
}
