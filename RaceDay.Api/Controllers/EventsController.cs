using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Creating, viewing, updating and deleting events.</summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;
    public EventsController(AppDbContext context) => _context = context;

    private static readonly string[] ValidEventTypes = { "Run", "Walk", "Cycle" };

    /// <summary>Lists all available events.</summary>
    /// <response code="200">Events returned.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult GetEvents()
    {
        var events = _context.Events
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventType,
                e.OrganiserId,
                Organiser = e.Organiser!.FirstName + " " + e.Organiser.LastName
            })
            .ToList();

        return Ok(events);
    }

    /// <summary>Returns one event together with its categories.</summary>
    /// <response code="200">Event returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("{id}")]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public IActionResult GetEvent(int id)
    {
        var item = _context.Events
            .Where(e => e.EventId == id)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventType,
                e.OrganiserId,
                Organiser = e.Organiser!.FirstName + " " + e.Organiser.LastName,
                Categories = e.Categories.Select(c => new { c.CategoryId, c.Name, c.Description })
            })
            .SingleOrDefault();

        if (item is null) return NotFound("Event not found.");

        return Ok(item);
    }

    /// <summary>Creates a new event. Organisers only.</summary>
    /// <remarks>The organiser is taken from the session, so an event is always created under the logged-in organiser's name.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Required details missing, or an invalid event type or distance.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [RequireRole("Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult CreateEvent(EventDto request)
    {
        var error = Validate(request);
        if (error is not null) return BadRequest(error);

        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = new Event
        {
            OrganiserId = organiserId,
            Name = request.Name,
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location,
            Distance = request.Distance,
            EventType = request.EventType
        };

        _context.Events.Add(item);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetEvent), new { id = item.EventId }, new
        {
            item.EventId, item.Name, item.Description, item.EventDate,
            item.Location, item.Distance, item.EventType, item.OrganiserId
        });
    }

    /// <summary>Updates an event. Organisers only, and only their own events.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Required details missing, or an invalid event type or distance.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPut("{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult UpdateEvent(int id, EventDto request)
    {
        var error = Validate(request);
        if (error is not null) return BadRequest(error);

        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = _context.Events.Find(id);
        if (item is null) return NotFound("Event not found.");
        if (item.OrganiserId != organiserId)
            return StatusCode(403, "You can only edit your own events.");

        item.Name = request.Name;
        item.Description = request.Description;
        item.EventDate = request.EventDate;
        item.Location = request.Location;
        item.Distance = request.Distance;
        item.EventType = request.EventType;

        _context.SaveChanges();

        return Ok(new
        {
            item.EventId, item.Name, item.Description, item.EventDate,
            item.Location, item.Distance, item.EventType, item.OrganiserId
        });
    }

    /// <summary>Deletes an event. Organisers only, and only their own events.</summary>
    /// <remarks>
    /// Blocked while the event still has enrolments, so entrant records are never silently lost.
    /// Categories belonging to the event are removed with it.
    /// </remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="400">The event still has enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpDelete("{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult DeleteEvent(int id)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = _context.Events.Find(id);
        if (item is null) return NotFound("Event not found.");
        if (item.OrganiserId != organiserId)
            return StatusCode(403, "You can only delete your own events.");

        if (_context.Enrolments.Any(e => e.EventId == id))
            return BadRequest("This event has enrolments and cannot be deleted.");

        _context.Events.Remove(item);
        _context.SaveChanges();

        return NoContent();
    }

    private static string? Validate(EventDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Event name is required.";
        if (string.IsNullOrWhiteSpace(request.Location)) return "Location is required.";
        if (request.Distance <= 0) return "Distance must be greater than zero.";
        if (!ValidEventTypes.Contains(request.EventType))
            return "Event type must be Run, Walk or Cycle.";
        return null;
    }
}
