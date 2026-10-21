using System.Net;
using System.Net.Http.Json;
using RaceDay.Mvc.Infrastructure;

namespace RaceDay.Mvc.Services;

/// <summary>
/// Wraps HttpClient for the Part 2 API.
///
/// The API keeps its own session (UserId and Role) behind a cookie. Because the
/// MVC server calls the API on the user's behalf, that cookie is captured at
/// login, stored in the user's MVC session, and attached to every later request.
/// That way each browser user has their own API session and the API's
/// role and ownership checks keep working unchanged.
/// </summary>
public class ApiClient : IApiClient
{
    /// <summary>Name of the API's session cookie (ASP.NET Core default).</summary>
    public const string ApiSessionCookieName = ".AspNetCore.Session";

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiClient(IHttpClientFactory factory, IHttpContextAccessor httpContextAccessor)
    {
        _http = factory.CreateClient("RaceDayApi");
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint));
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public Task<HttpResponseMessage> PostAsync<T>(string endpoint, T body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(body) });

    public Task<HttpResponseMessage> PutAsync<T>(string endpoint, T body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, endpoint) { Content = JsonContent.Create(body) });

    public Task<HttpResponseMessage> DeleteAsync(string endpoint) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, endpoint));

    public Task<HttpResponseMessage> PostFileAsync(string endpoint, MultipartFormDataContent content) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content });

    public Task<HttpResponseMessage> PutFileAsync(string endpoint, MultipartFormDataContent content) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, endpoint) { Content = content });

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        // 1. Attach this user's API session cookie, if they have one.
        string? apiCookie = Session?.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(apiCookie))
            request.Headers.Add("Cookie", apiCookie);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiUnavailableException("The RaceDay API could not be reached.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiUnavailableException("The RaceDay API took too long to respond.", ex);
        }

        // 2. Keep the newest API session cookie (the API issues it on login).
        CaptureApiCookie(response);

        // 3. Logged in on the MVC side but the API no longer knows us: session expired.
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiCookie))
            throw new ApiSessionExpiredException();

        return response;
    }

    private void CaptureApiCookie(HttpResponseMessage response)
    {
        if (Session is null) return;
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies)) return;

        foreach (string header in setCookies)
        {
            // "Set-Cookie: .AspNetCore.Session=CfDJ8...; path=/; samesite=lax; httponly"
            string nameValue = header.Split(';', 2)[0].Trim();
            if (nameValue.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal))
            {
                Session.SetString(SessionKeys.ApiCookie, nameValue);
            }
        }
    }
}
