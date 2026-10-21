using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class RegisterViewModel
{
    [Required, StringLength(50), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6,
        ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose whether you are an Organiser or a Participant.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Choose Organiser or Participant.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

/// <summary>What POST /api/auth/login returns.</summary>
public class LoginResult
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
