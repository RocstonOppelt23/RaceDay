using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and logout.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    public AuthController(AppDbContext context) => _context = context;

    /// <summary>Registers a new user account as an Organiser or a Participant.</summary>
    /// <remarks>The password is hashed with BCrypt before it is stored; the plain password is never saved.</remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Missing details, or the role is not Organiser or Participant.</response>
    /// <response code="409">That email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public IActionResult Register(RegisterDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest("First name and last name are required.");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        if (request.Role is not ("Organiser" or "Participant"))
            return BadRequest("Role must be Organiser or Participant.");

        if (_context.Users.Any(u => u.Email == request.Email))
            return Conflict("That email address is already registered.");

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role
        };

        // Every user gets an empty profile row they can fill in later.
        user.Profile = new UserProfile();

        _context.Users.Add(user);
        _context.SaveChanges();

        return StatusCode(201, new { user.UserId, user.FirstName, user.LastName, user.Email, user.Role });
    }

    /// <summary>Authenticates a user and starts a session.</summary>
    /// <remarks>The session stores the user's id and role; every protected endpoint reads them from there.</remarks>
    /// <response code="200">Logged in. Returns the user's id, name and role.</response>
    /// <response code="400">Email or password missing.</response>
    /// <response code="401">Email not found, or the password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public IActionResult Login(LoginDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        var user = _context.Users.SingleOrDefault(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new { user.UserId, user.FirstName, user.LastName, user.Role });
    }

    /// <summary>Ends the current session.</summary>
    /// <response code="200">Logged out.</response>
    /// <response code="401">No one is logged in.</response>
    [HttpPost("logout")]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok("Logged out.");
    }
}
