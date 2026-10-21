using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

/// <summary>What GET /api/users/profile returns.</summary>
public class ProfileViewModel
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>The Edit Profile form.</summary>
public class ProfileFormViewModel
{
    [Required, StringLength(50), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(30), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }
}
