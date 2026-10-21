using System.Net;
using Microsoft.AspNetCore.Http;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using Xunit;

namespace RaceDay.Mvc.Tests;

public class ApiClientTests
{
    [Fact]
    public async Task Login_CapturesTheApiSessionCookieIntoTheMvcSession()
    {
        var handler = new StubHandler(_ =>
        {
            var response = StubHandler.Json(HttpStatusCode.OK, "{\"userId\":1}");
            response.Headers.Add("Set-Cookie", ".AspNetCore.Session=abc123; path=/; samesite=lax; httponly");
            return response;
        });
        var client = new ApiClient(new StubHttpClientFactory(handler), TestContext.AccessorWithSession(out var session));

        await client.PostAsync("/api/auth/login", new { Email = "a@b.co.za", Password = "x" });

        Assert.Equal(".AspNetCore.Session=abc123", session.GetString(SessionKeys.ApiCookie));
    }

    [Fact]
    public async Task LaterRequests_SendTheStoredApiCookie()
    {
        var handler = new StubHandler(_ => StubHandler.Json(HttpStatusCode.OK, "[]"));
        var client = new ApiClient(new StubHttpClientFactory(handler), TestContext.AccessorWithSession(out var session));
        session.SetString(SessionKeys.ApiCookie, ".AspNetCore.Session=abc123");

        await client.GetAsync<List<object>>("/api/events");

        var sent = Assert.Single(handler.Requests);
        Assert.Contains(".AspNetCore.Session=abc123", sent.Headers.GetValues("Cookie"));
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenTheApiSaysNotFound()
    {
        var handler = new StubHandler(_ => StubHandler.Json(HttpStatusCode.NotFound, "\"Event not found.\""));
        var client = new ApiClient(new StubHttpClientFactory(handler), TestContext.AccessorWithSession(out _));

        Assert.Null(await client.GetAsync<object>("/api/events/99"));
    }

    [Fact]
    public async Task Unauthorised_ForALoggedInUser_ThrowsSessionExpired()
    {
        var handler = new StubHandler(_ => StubHandler.Json(HttpStatusCode.Unauthorized, "\"You must be logged in.\""));
        var client = new ApiClient(new StubHttpClientFactory(handler), TestContext.AccessorWithSession(out var session));
        session.SetString(SessionKeys.ApiCookie, ".AspNetCore.Session=old");

        await Assert.ThrowsAsync<ApiSessionExpiredException>(() => client.GetAsync<object>("/api/events"));
    }

    [Fact]
    public async Task UnreachableApi_ThrowsApiUnavailable()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("Connection refused"));
        var client = new ApiClient(new StubHttpClientFactory(handler), TestContext.AccessorWithSession(out _));

        await Assert.ThrowsAsync<ApiUnavailableException>(() => client.GetAsync<object>("/api/events/upcoming"));
    }

    [Theory]
    [InlineData("\"You are already entered for this event and category.\"", "You are already entered for this event and category.")]
    [InlineData("{\"title\":\"Bad\",\"errors\":{\"Distance\":[\"Distance must be greater than zero.\"]}}", "Distance must be greater than zero.")]
    [InlineData("", "fallback")]
    public async Task ApiErrors_ReadsTheMessageTheApiSent(string body, string expected)
    {
        var response = StubHandler.Json(HttpStatusCode.BadRequest, body);

        Assert.Equal(expected, await ApiErrors.ReadMessageAsync(response, "fallback"));
    }
}
