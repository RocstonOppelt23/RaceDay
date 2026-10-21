namespace RaceDay.Mvc.Services;

/// <summary>
/// The ONLY way the MVC app talks to data: every controller calls the Part 2 API
/// through this client. The MVC project has no database of its own.
/// </summary>
public interface IApiClient
{
    /// <summary>GET and read the JSON body. Returns default when the API answers 404/400/403.</summary>
    Task<T?> GetAsync<T>(string endpoint);

    Task<HttpResponseMessage> PostAsync<T>(string endpoint, T body);
    Task<HttpResponseMessage> PutAsync<T>(string endpoint, T body);
    Task<HttpResponseMessage> DeleteAsync(string endpoint);

    /// <summary>POST a multipart form (used for image uploads).</summary>
    Task<HttpResponseMessage> PostFileAsync(string endpoint, MultipartFormDataContent content);

    /// <summary>PUT a multipart form (used when editing an event with a new banner).</summary>
    Task<HttpResponseMessage> PutFileAsync(string endpoint, MultipartFormDataContent content);
}
