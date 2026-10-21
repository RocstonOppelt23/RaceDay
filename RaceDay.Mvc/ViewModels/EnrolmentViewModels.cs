using System.Text.Json.Serialization;

namespace RaceDay.Mvc.ViewModels;

/// <summary>
/// An enrolment as returned by GET /api/enrolments/my (participant)
/// or GET /api/events/{id}/enrolments (organiser).
/// </summary>
public class EnrolmentViewModel
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }

    [JsonPropertyName("event")]
    public string EventName { get; set; } = string.Empty;

    public DateTime? EventDate { get; set; }

    public int ParticipantId { get; set; }

    [JsonPropertyName("participant")]
    public string ParticipantName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    [JsonPropertyName("category")]
    public string CategoryName { get; set; } = string.Empty;

    public DateTime EnrolmentDate { get; set; }

    /// <summary>"Active" or "Cancelled" in the Part 2 API.</summary>
    public string Status { get; set; } = string.Empty;

    public bool IsActive => Status == "Active";
}

/// <summary>Organiser view of one event's entrants, with any captured results.</summary>
public class EventEnrolmentsViewModel
{
    public EventViewModel Event { get; set; } = new();
    public List<EnrolmentViewModel> Enrolments { get; set; } = new();

    /// <summary>Results keyed by enrolment id.</summary>
    public Dictionary<int, ResultViewModel> Results { get; set; } = new();
}

/// <summary>A row on the organiser's events list (Enrolments and Results menus).</summary>
public class OrganiserEventSummary
{
    public EventViewModel Event { get; set; } = new();
    public int EnrolmentCount { get; set; }
    public int ResultCount { get; set; }
}
