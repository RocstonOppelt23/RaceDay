using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

/// <summary>Participant: view and update their profile, including a profile picture.</summary>
[RequireSessionRole(Roles.Participant)]
public class ProfileController : Controller
{
    private readonly IApiClient _api;

    private const long MaxPictureBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };

    public ProfileController(IApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var profile = await _api.GetAsync<ProfileViewModel>("/api/users/profile");
        if (profile is null) return NotFound();
        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var profile = await _api.GetAsync<ProfileViewModel>("/api/users/profile");
        if (profile is null) return NotFound();

        return View(new ProfileFormViewModel
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Email = profile.Email,
            PhoneNumber = profile.PhoneNumber,
            DateOfBirth = profile.DateOfBirth,
            ProfilePictureUrl = profile.ProfilePictureUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileFormViewModel model, IFormFile? profilePicture)
    {
        if (profilePicture is { Length: > 0 })
        {
            if (profilePicture.Length > MaxPictureBytes)
                ModelState.AddModelError("profilePicture", "The picture must be 5 MB or smaller.");
            else if (!AllowedImageTypes.Contains(profilePicture.ContentType.ToLowerInvariant()))
                ModelState.AddModelError("profilePicture", "The picture must be a JPG, PNG, WEBP or GIF image.");
        }

        if (model.DateOfBirth is DateOnly dob && dob > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth cannot be in the future.");

        if (!ModelState.IsValid) return View(model);

        // 1. The text details (the API keeps the existing picture URL we send back).
        var response = await _api.PutAsync("/api/users/profile", new
        {
            model.FirstName,
            model.LastName,
            model.Email,
            model.PhoneNumber,
            model.DateOfBirth,
            model.ProfilePictureUrl
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ApiErrors.ReadMessageAsync(response, "Could not update your profile."));
            return View(model);
        }

        // 2. The picture, if one was chosen: posted to the API, which stores it in Azure Blob Storage.
        if (profilePicture is { Length: > 0 })
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(profilePicture.OpenReadStream());
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(profilePicture.ContentType);
            content.Add(fileContent, "profilePicture", profilePicture.FileName);

            var upload = await _api.PostFileAsync("/api/users/profile/picture", content);
            if (!upload.IsSuccessStatusCode)
            {
                TempData["Error"] = "Your details were saved, but the picture could not be uploaded: " +
                    await ApiErrors.ReadMessageAsync(upload, "please try a different image.");
                return RedirectToAction(nameof(Index));
            }
        }

        // Keep the name in the navigation bar in step with the profile.
        HttpContext.Session.SetString(SessionKeys.FullName, $"{model.FirstName} {model.LastName}".Trim());

        TempData["Message"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }
}
