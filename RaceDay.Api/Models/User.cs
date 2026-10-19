namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // One profile per user
    public UserProfile? Profile { get; set; }

    // Events this user organises
    public ICollection<Event> Events { get; set; } = new List<Event>();

    // Events this user has entered
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
