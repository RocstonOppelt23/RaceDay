using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Age and distance categories belonging to an event.</summary>
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    public CategoriesController(AppDbContext context) => _context = context;

    /// <summary>Lists the categories belonging to one event.</summary>
    /// <response code="200">Categories returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("api/events/{eventId}/categories")]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public IActionResult GetCategories(int eventId)
    {
        if (!_context.Events.Any(e => e.EventId == eventId))
            return NotFound("Event not found.");

        var categories = _context.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new { c.CategoryId, c.Name, c.Description, c.EventId })
            .ToList();

        return Ok(categories);
    }

    /// <summary>Creates a category for an event. Organisers only, and only their own events.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Category name missing.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("api/events/{eventId}/categories")]
    [RequireRole("Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult CreateCategory(int eventId, CategoryDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Category name is required.");

        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = _context.Events.Find(eventId);
        if (ev is null) return NotFound("Event not found.");
        if (ev.OrganiserId != organiserId)
            return StatusCode(403, "You can only add categories to your own events.");

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name,
            Description = request.Description
        };

        _context.Categories.Add(category);
        _context.SaveChanges();

        return StatusCode(201, new { category.CategoryId, category.Name, category.Description, category.EventId });
    }

    /// <summary>Updates a category. Organisers only, and only on their own events.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Category name missing.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of the event.</response>
    /// <response code="404">No category with that id.</response>
    [HttpPut("api/categories/{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult UpdateCategory(int id, CategoryDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Category name is required.");

        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var category = _context.Categories.Find(id);
        if (category is null) return NotFound("Category not found.");

        var ev = _context.Events.Find(category.EventId);
        if (ev is null || ev.OrganiserId != organiserId)
            return StatusCode(403, "You can only edit categories on your own events.");

        category.Name = request.Name;
        category.Description = request.Description;
        _context.SaveChanges();

        return Ok(new { category.CategoryId, category.Name, category.Description, category.EventId });
    }

    /// <summary>Deletes a category. Organisers only, and only on their own events.</summary>
    /// <remarks>Blocked while participants are still entered under that category.</remarks>
    /// <response code="204">Category deleted.</response>
    /// <response code="400">The category still has enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of the event.</response>
    /// <response code="404">No category with that id.</response>
    [HttpDelete("api/categories/{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult DeleteCategory(int id)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var category = _context.Categories.Find(id);
        if (category is null) return NotFound("Category not found.");

        var ev = _context.Events.Find(category.EventId);
        if (ev is null || ev.OrganiserId != organiserId)
            return StatusCode(403, "You can only delete categories on your own events.");

        if (_context.Enrolments.Any(e => e.CategoryId == id))
            return BadRequest("This category has enrolments and cannot be deleted.");

        _context.Categories.Remove(category);
        _context.SaveChanges();

        return NoContent();
    }
}
