namespace RaceDay.Mvc.Infrastructure;

/// <summary>Names of the values kept in the MVC session after login.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";

    /// <summary>The API's own session cookie, forwarded on every API call.</summary>
    public const string ApiCookie = "ApiSessionCookie";
}

/// <summary>The two roles the API knows about.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
