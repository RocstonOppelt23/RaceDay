using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Mvc.ViewModels;

public class ResultViewModel
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }

    [JsonPropertyName("event")]
    public string EventName { get; set; } = string.Empty;

    public DateTime? EventDate { get; set; }

    [JsonPropertyName("participant")]
    public string ParticipantName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string CategoryName { get; set; } = string.Empty;

    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
    public int TotalFinishers { get; set; }

    public string PositionText => TotalFinishers > 0
        ? $"{Ordinal(FinishingPosition)} out of {TotalFinishers}"
        : Ordinal(FinishingPosition);

    public static string Ordinal(int n)
    {
        int lastTwo = n % 100;
        if (lastTwo is 11 or 12 or 13) return $"{n}th";
        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }
}

public class ResultFormViewModel
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the finish time.")]
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use hh:mm:ss, for example 00:52:35.")]
    [Display(Name = "Finish time (hh:mm:ss)")]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    [Display(Name = "Finishing position")]
    public int FinishingPosition { get; set; } = 1;
}

public class EventResultsViewModel
{
    public EventViewModel Event { get; set; } = new();
    public List<ResultViewModel> Results { get; set; } = new();
}
