using System.Text.Json.Serialization;

namespace RaceDay.Mvc.ViewModels;

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

    public string Status { get; set; } = string.Empty;

    public bool IsActive => Status == "Active";
}

public class EventEnrolmentsViewModel
{
    public EventViewModel Event { get; set; } = new();
    public List<EnrolmentViewModel> Enrolments { get; set; } = new();

    public Dictionary<int, ResultViewModel> Results { get; set; } = new();
}

public class OrganiserEventSummary
{
    public EventViewModel Event { get; set; } = new();
    public int EnrolmentCount { get; set; }
    public int ResultCount { get; set; }
}
