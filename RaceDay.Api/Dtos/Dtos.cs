namespace RaceDay.Api.Dtos;

public record RegisterDto(string FirstName, string LastName, string Email, string Password, string Role);
public record LoginDto(string Email, string Password);

public record UpdateProfileDto(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? ProfilePictureUrl);

public record EventDto(
    string Name,
    string? Description,
    DateTime EventDate,
    string Location,
    decimal Distance,
    string EventType);

public record CategoryDto(string Name, string? Description);

public record EnrolmentDto(int CategoryId);

public record ResultDto(TimeSpan FinishTime, int FinishingPosition);
