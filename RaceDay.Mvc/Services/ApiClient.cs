using System.Net;
using System.Net.Http.Json;
using RaceDay.Mvc.Infrastructure;

namespace RaceDay.Mvc.Services;

public class ApiClient : IApiClient
{
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

        CaptureApiCookie(response);

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
            string nameValue = header.Split(';', 2)[0].Trim();
            if (nameValue.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal))
            {
                Session.SetString(SessionKeys.ApiCookie, nameValue);
            }
        }
    }
}
