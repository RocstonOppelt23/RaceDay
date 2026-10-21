using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Mvc.ViewModels;

public class EventViewModel
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserId { get; set; }

    [JsonPropertyName("organiser")]
    public string OrganiserName { get; set; } = string.Empty;

    public string? BannerImageUrl { get; set; }
    public List<CategoryViewModel> Categories { get; set; } = new();

    public bool IsUpcoming => EventDate.Date >= DateTime.Today;
}

public class EventFormViewModel : IValidatableObject
{
    public int EventId { get; set; }

    [Required, StringLength(150), Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, Display(Name = "Date and start time")]
    [DataType(DataType.DateTime)]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30).AddHours(7);

    [Required, StringLength(255)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 500, ErrorMessage = "Distance must be between 0.1 and 500 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Required]
    [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public string? ExistingBannerUrl { get; set; }

    public bool IsNew => EventId == 0;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsNew && EventDate < DateTime.Now)
            yield return new ValidationResult("A new event must be in the future.", new[] { nameof(EventDate) });
    }
}

public class CategoryViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EventId { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }

    [Required, StringLength(100), Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class CategoryManageViewModel
{
    public EventViewModel Event { get; set; } = new();
    public List<CategoryViewModel> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventViewModel Event { get; set; } = new();
    public string? Role { get; set; }
    public bool IsOwner { get; set; }

    public HashSet<int> EnteredCategoryIds { get; set; } = new();
}

public class BrowseEventsViewModel
{
    public List<EventViewModel> Events { get; set; } = new();
    public string? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public string? Search { get; set; }
}

public class DashboardViewModel
{
    public string OrganiserName { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public int UpcomingCount { get; set; }
    public int PastCount { get; set; }
    public int TotalEnrolments { get; set; }
    public List<EventViewModel> UpcomingEvents { get; set; } = new();
    public Dictionary<int, int> EnrolmentCounts { get; set; } = new();
}
