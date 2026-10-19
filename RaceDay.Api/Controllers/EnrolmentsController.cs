using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Participants entering events, and organisers viewing their entrants.</summary>
[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    public EnrolmentsController(AppDbContext context) => _context = context;

    /// <summary>Enters the logged-in participant into an event under one of its categories.</summary>
    /// <remarks>The participant is taken from the session. One entry per participant, per event, per category.</remarks>
    /// <response code="201">Enrolment created.</response>
    /// <response code="400">The category does not belong to this event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">No event or category with that id.</response>
    /// <response code="409">Already entered for this event and category.</response>
    [HttpPost("api/events/{eventId}/enrolments")]
    [RequireRole("Participant")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public IActionResult Enrol(int eventId, EnrolmentDto request)
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        if (!_context.Events.Any(e => e.EventId == eventId))
            return NotFound("Event not found.");

        var category = _context.Categories.Find(request.CategoryId);
        if (category is null) return NotFound("Category not found.");

        if (category.EventId != eventId)
            return BadRequest("That category does not belong to this event.");

        bool duplicate = _context.Enrolments.Any(e =>
            e.ParticipantId == participantId &&
            e.EventId == eventId &&
            e.CategoryId == request.CategoryId);

        if (duplicate)
            return Conflict("You are already entered for this event and category.");

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = eventId,
            CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Active"
        };

        _context.Enrolments.Add(enrolment);
        _context.SaveChanges();

        return StatusCode(201, new
        {
            enrolment.EnrolmentId,
            enrolment.ParticipantId,
            enrolment.EventId,
            enrolment.CategoryId,
            enrolment.EnrolmentDate,
            enrolment.Status
        });
    }

    /// <summary>Lists the logged-in participant's own enrolments.</summary>
    /// <response code="200">Enrolments returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("api/enrolments/my")]
    [RequireRole("Participant")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult GetMyEnrolments()
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        var enrolments = _context.Enrolments
            .Where(e => e.ParticipantId == participantId)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EventId,
                Event = e.Event!.Name,
                e.Event.EventDate,
                e.CategoryId,
                Category = e.Category!.Name,
                e.EnrolmentDate,
                e.Status
            })
            .ToList();

        return Ok(enrolments);
    }

    /// <summary>Lists everyone entered for one event. Organisers only, and only their own events.</summary>
    /// <response code="200">Enrolments returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("api/events/{eventId}/enrolments")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult GetEventEnrolments(int eventId)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = _context.Events.Find(eventId);
        if (ev is null) return NotFound("Event not found.");
        if (ev.OrganiserId != organiserId)
            return StatusCode(403, "You can only view enrolments for your own events.");

        var enrolments = _context.Enrolments
            .Where(e => e.EventId == eventId)
            .Select(e => new
            {
                e.EnrolmentId,
                e.ParticipantId,
                Participant = e.Participant!.FirstName + " " + e.Participant.LastName,
                e.CategoryId,
                Category = e.Category!.Name,
                e.EnrolmentDate,
                e.Status
            })
            .ToList();

        return Ok(enrolments);
    }

    /// <summary>Returns one enrolment.</summary>
    /// <remarks>A Participant may only view their own; an Organiser only those on their own events.</remarks>
    /// <response code="200">Enrolment returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not your enrolment, and not an event you organise.</response>
    /// <response code="404">No enrolment with that id.</response>
    [HttpGet("api/enrolments/{id}")]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult GetEnrolment(int id)
    {
        int userId = HttpContext.Session.GetInt32("UserId")!.Value;
        string role = HttpContext.Session.GetString("Role")!;

        var enrolment = _context.Enrolments
            .Where(e => e.EnrolmentId == id)
            .Select(e => new
            {
                e.EnrolmentId,
                e.ParticipantId,
                Participant = e.Participant!.FirstName + " " + e.Participant.LastName,
                e.EventId,
                Event = e.Event!.Name,
                OrganiserId = e.Event.OrganiserId,
                e.CategoryId,
                Category = e.Category!.Name,
                e.EnrolmentDate,
                e.Status
            })
            .SingleOrDefault();

        if (enrolment is null) return NotFound("Enrolment not found.");

        bool allowed = role == "Participant"
            ? enrolment.ParticipantId == userId
            : enrolment.OrganiserId == userId;

        if (!allowed) return StatusCode(403, "You cannot view this enrolment.");

        return Ok(enrolment);
    }

    /// <summary>Cancels the logged-in participant's own enrolment.</summary>
    /// <remarks>
    /// This is a soft cancel: the row stays and its Status becomes Cancelled, so the
    /// entry history is preserved and any result captured against it is not lost.
    /// </remarks>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser, or not your enrolment.</response>
    /// <response code="404">No enrolment with that id.</response>
    [HttpDelete("api/enrolments/{id}")]
    [RequireRole("Participant")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult CancelEnrolment(int id)
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        var enrolment = _context.Enrolments.Find(id);
        if (enrolment is null) return NotFound("Enrolment not found.");
        if (enrolment.ParticipantId != participantId)
            return StatusCode(403, "You can only cancel your own enrolments.");

        enrolment.Status = "Cancelled";
        _context.SaveChanges();

        return NoContent();
    }
}
