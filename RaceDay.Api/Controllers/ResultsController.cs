using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Finish times and finishing positions, captured after an event.</summary>
[ApiController]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _context;
    public ResultsController(AppDbContext context) => _context = context;

    /// <summary>Records a result against an enrolment. Organisers only, and only on their own events.</summary>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid finishing position, or this enrolment already has a result.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of the event.</response>
    /// <response code="404">No enrolment with that id.</response>
    [HttpPost("api/enrolments/{enrolmentId}/result")]
    [RequireRole("Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult CaptureResult(int enrolmentId, ResultDto request)
    {
        if (request.FinishingPosition <= 0)
            return BadRequest("Finishing position must be greater than zero.");

        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var enrolment = _context.Enrolments
            .Where(e => e.EnrolmentId == enrolmentId)
            .Select(e => new { e.EnrolmentId, e.Event!.OrganiserId })
            .SingleOrDefault();

        if (enrolment is null) return NotFound("Enrolment not found.");
        if (enrolment.OrganiserId != organiserId)
            return StatusCode(403, "You can only capture results for your own events.");

        if (_context.Results.Any(r => r.EnrolmentId == enrolmentId))
            return BadRequest("This enrolment already has a result.");

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            FinishingPosition = request.FinishingPosition
        };

        _context.Results.Add(result);
        _context.SaveChanges();

        return StatusCode(201, new
        {
            result.ResultId, result.EnrolmentId, result.FinishTime, result.FinishingPosition
        });
    }

    /// <summary>Returns the logged-in participant's own race history.</summary>
    /// <response code="200">Results returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("api/results/my")]
    [RequireRole("Participant")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult GetMyResults()
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        // Filtered through the enrolment, so a participant only ever sees their own.
        var results = _context.Results
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                Event = r.Enrolment!.Event!.Name,
                r.Enrolment.Event.EventDate,
                Category = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.FinishingPosition
            })
            .ToList();

        return Ok(results);
    }

    /// <summary>Lists all results for one event. Organisers only, and only their own events.</summary>
    /// <response code="200">Results returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("api/events/{eventId}/results")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult GetEventResults(int eventId)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = _context.Events.Find(eventId);
        if (ev is null) return NotFound("Event not found.");
        if (ev.OrganiserId != organiserId)
            return StatusCode(403, "You can only view results for your own events.");

        var results = _context.Results
            .Where(r => r.Enrolment!.EventId == eventId)
            .OrderBy(r => r.FinishingPosition)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                Participant = r.Enrolment!.Participant!.FirstName + " " + r.Enrolment.Participant.LastName,
                Category = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.FinishingPosition
            })
            .ToList();

        return Ok(results);
    }

    /// <summary>Returns one result.</summary>
    /// <remarks>A Participant may only view their own; an Organiser only those on their own events.</remarks>
    /// <response code="200">Result returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not your result, and not an event you organise.</response>
    /// <response code="404">No result with that id.</response>
    [HttpGet("api/results/{id}")]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult GetResult(int id)
    {
        int userId = HttpContext.Session.GetInt32("UserId")!.Value;
        string role = HttpContext.Session.GetString("Role")!;

        var result = _context.Results
            .Where(r => r.ResultId == id)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                ParticipantId = r.Enrolment!.ParticipantId,
                Participant = r.Enrolment.Participant!.FirstName + " " + r.Enrolment.Participant.LastName,
                OrganiserId = r.Enrolment.Event!.OrganiserId,
                Event = r.Enrolment.Event.Name,
                Category = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.FinishingPosition
            })
            .SingleOrDefault();

        if (result is null) return NotFound("Result not found.");

        bool allowed = role == "Participant"
            ? result.ParticipantId == userId
            : result.OrganiserId == userId;

        if (!allowed) return StatusCode(403, "You cannot view this result.");

        return Ok(result);
    }

    /// <summary>Updates a result. Organisers only, and only on their own events.</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Invalid finishing position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of the event.</response>
    /// <response code="404">No result with that id.</response>
    [HttpPut("api/results/{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult UpdateResult(int id, ResultDto request)
    {
        if (request.FinishingPosition <= 0)
            return BadRequest("Finishing position must be greater than zero.");

        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var result = _context.Results.Find(id);
        if (result is null) return NotFound("Result not found.");

        if (!OrganiserOwnsResult(result.EnrolmentId, organiserId))
            return StatusCode(403, "You can only edit results for your own events.");

        result.FinishTime = request.FinishTime;
        result.FinishingPosition = request.FinishingPosition;
        _context.SaveChanges();

        return Ok(new { result.ResultId, result.EnrolmentId, result.FinishTime, result.FinishingPosition });
    }

    /// <summary>Removes a result. Organisers only, and only on their own events.</summary>
    /// <response code="204">Result removed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the organiser of the event.</response>
    /// <response code="404">No result with that id.</response>
    [HttpDelete("api/results/{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult DeleteResult(int id)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var result = _context.Results.Find(id);
        if (result is null) return NotFound("Result not found.");

        if (!OrganiserOwnsResult(result.EnrolmentId, organiserId))
            return StatusCode(403, "You can only delete results for your own events.");

        _context.Results.Remove(result);
        _context.SaveChanges();

        return NoContent();
    }

    /// <summary>True when the event behind this enrolment belongs to the given organiser.</summary>
    private bool OrganiserOwnsResult(int enrolmentId, int organiserId) =>
        _context.Enrolments.Any(e => e.EnrolmentId == enrolmentId && e.Event!.OrganiserId == organiserId);
}
