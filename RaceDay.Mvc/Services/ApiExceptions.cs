namespace RaceDay.Mvc.Services;

public class ApiUnavailableException : Exception
{
    public ApiUnavailableException(string message, Exception inner) : base(message, inner) { }
}

public class ApiSessionExpiredException : Exception
{
    public ApiSessionExpiredException() : base("Your session with the RaceDay service has expired.") { }
}
