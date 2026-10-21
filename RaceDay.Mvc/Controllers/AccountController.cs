using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

/// <summary>Register, log in and log out — all through the Part 2 API.</summary>
public class AccountController : Controller
{
    private readonly IApiClient _api;

    public AccountController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var response = await _api.PostAsync("/api/auth/register", new
        {
            model.FirstName,
            model.LastName,
            model.Email,
            model.Password,
            model.Role
        });

        if (!response.IsSuccessStatusCode)
        {
            string message = await ApiErrors.ReadMessageAsync(response,
                "Registration failed. Check your details and try again.");
            ModelState.AddModelError("", message);
            return View(model);
        }

        TempData["Message"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Already logged in: go straight to the right landing page.
        if (HttpContext.Session.GetString(SessionKeys.Role) is string role)
            return RedirectToLanding(role);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Start from a clean session so an old API cookie is never reused.
        HttpContext.Session.Clear();

        var response = await _api.PostAsync("/api/auth/login", new { model.Email, model.Password });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        if (result is null)
        {
            ModelState.AddModelError("", "The login service returned an unexpected answer. Please try again.");
            return View(model);
        }

        HttpContext.Session.SetInt32(SessionKeys.UserId, result.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, result.Role);
        HttpContext.Session.SetString(SessionKeys.FullName, result.FullName);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToLanding(result.Role);
    }

    public async Task<IActionResult> Logout()
    {
        // End the API session too (best effort), then clear the MVC session.
        if (HttpContext.Session.GetString(SessionKeys.ApiCookie) is not null)
        {
            try { await _api.PostAsync<object?>("/api/auth/logout", null); }
            catch (ApiUnavailableException) { /* still log out locally */ }
            catch (ApiSessionExpiredException) { /* already gone on the API side */ }
        }

        HttpContext.Session.Clear();
        TempData["Message"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLanding(string role) =>
        role == Roles.Organiser
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Browse", "Events");
}
