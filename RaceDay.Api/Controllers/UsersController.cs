using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Viewing and updating user profiles.</summary>
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsersController(AppDbContext context) => _context = context;

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <remarks>The user id is read from the session, never from the request, so a user can only see their own record.</remarks>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The account no longer exists.</response>
    [HttpGet("profile")]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public IActionResult GetMyProfile()
    {
        int userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var user = _context.Users
            .Include(u => u.Profile)
            .SingleOrDefault(u => u.UserId == userId);

        if (user is null) return NotFound("Profile not found.");

        return Ok(new
        {
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            PhoneNumber = user.Profile?.PhoneNumber,
            DateOfBirth = user.Profile?.DateOfBirth,
            ProfilePictureUrl = user.Profile?.ProfilePictureUrl
        });
    }

    /// <summary>Updates the logged-in user's own profile.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Required details missing, or the email is already used by someone else.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("profile")]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public IActionResult UpdateMyProfile(UpdateProfileDto request)
    {
        int userId = HttpContext.Session.GetInt32("UserId")!.Value;

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("First name, last name and email are required.");

        var user = _context.Users
            .Include(u => u.Profile)
            .SingleOrDefault(u => u.UserId == userId);

        if (user is null) return NotFound("Profile not found.");

        // Email is unique, so block a clash with another account.
        if (_context.Users.Any(u => u.Email == request.Email && u.UserId != userId))
            return BadRequest("That email address is already in use.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;

        user.Profile ??= new UserProfile { UserId = userId };
        user.Profile.PhoneNumber = request.PhoneNumber;
        user.Profile.DateOfBirth = request.DateOfBirth;
        user.Profile.ProfilePictureUrl = request.ProfilePictureUrl;

        _context.SaveChanges();

        return Ok(new
        {
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.Profile.PhoneNumber,
            user.Profile.DateOfBirth,
            user.Profile.ProfilePictureUrl
        });
    }

    /// <summary>Returns another user's basic information. Organisers only.</summary>
    /// <remarks>Used by an Organiser to look up a participant who has entered one of their events.</remarks>
    /// <response code="200">User returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    /// <response code="404">No user with that id.</response>
    [HttpGet("{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult GetUser(int id)
    {
        var user = _context.Users
            .Where(u => u.UserId == id)
            .Select(u => new { u.UserId, u.FirstName, u.LastName, u.Email, u.Role })
            .SingleOrDefault();

        if (user is null) return NotFound("User not found.");

        return Ok(user);
    }
}
