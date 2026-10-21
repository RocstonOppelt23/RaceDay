namespace RaceDay.Mvc.Services;

/// <summary>The API could not be reached (not running, wrong ApiBaseUrl, or timed out).</summary>
public class ApiUnavailableException : Exception
{
    public ApiUnavailableException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>The API answered 401 for a user the MVC app thought was logged in.</summary>
public class ApiSessionExpiredException : Exception
{
    public ApiSessionExpiredException() : base("Your session with the RaceDay service has expired.") { }
}
