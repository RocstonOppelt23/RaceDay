namespace RaceDay.Mvc.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint);

    Task<HttpResponseMessage> PostAsync<T>(string endpoint, T body);
    Task<HttpResponseMessage> PutAsync<T>(string endpoint, T body);
    Task<HttpResponseMessage> DeleteAsync(string endpoint);

    Task<HttpResponseMessage> PostFileAsync(string endpoint, MultipartFormDataContent content);

    Task<HttpResponseMessage> PutFileAsync(string endpoint, MultipartFormDataContent content);
}
