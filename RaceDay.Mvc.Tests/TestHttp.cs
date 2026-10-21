using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Tests;

public class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

    public List<HttpRequestMessage> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(_respond(request));
    }

    public static HttpResponseMessage Json(HttpStatusCode code, string json) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

public class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name) =>
        new(_handler, disposeHandler: false) { BaseAddress = new Uri("https://api.test") };
}

public static class TestContext
{
    public static IHttpContextAccessor AccessorWithSession(out ISession session)
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        session = httpContext.Session;
        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
