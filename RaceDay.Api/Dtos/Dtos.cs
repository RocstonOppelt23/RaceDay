namespace RaceDay.Api.Dtos;

// ---------- Authentication ----------
public record RegisterDto(string FirstName, string LastName, string Email, string Password, string Role);
public record LoginDto(string Email, string Password);

// ---------- User profile ----------
public record UpdateProfileDto(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? ProfilePictureUrl);

// ---------- Events ----------
public record EventDto(
    string Name,
    string? Description,
    DateTime EventDate,
    string Location,
    decimal Distance,
    string EventType);

// ---------- Categories ----------
public record CategoryDto(string Name, string? Description);

// ---------- Enrolments ----------
public record EnrolmentDto(int CategoryId);

// ---------- Results ----------
public record ResultDto(TimeSpan FinishTime, int FinishingPosition);
