using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Mvc.Controllers;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;
using Xunit;

namespace RaceDay.Mvc.Tests;

public class AccountControllerTests
{
    private class FakeApi : IApiClient
    {
        public HttpResponseMessage NextResponse { get; set; } = new(HttpStatusCode.OK);
        public string? LastEndpoint { get; private set; }

        public Task<T?> GetAsync<T>(string endpoint) => Task.FromResult<T?>(default);
        public Task<HttpResponseMessage> PostAsync<T>(string endpoint, T body) { LastEndpoint = endpoint; return Task.FromResult(NextResponse); }
        public Task<HttpResponseMessage> PutAsync<T>(string endpoint, T body) => Task.FromResult(NextResponse);
        public Task<HttpResponseMessage> DeleteAsync(string endpoint) => Task.FromResult(NextResponse);
        public Task<HttpResponseMessage> PostFileAsync(string endpoint, MultipartFormDataContent content) => Task.FromResult(NextResponse);
        public Task<HttpResponseMessage> PutFileAsync(string endpoint, MultipartFormDataContent content) => Task.FromResult(NextResponse);
    }

    private static AccountController ControllerWith(FakeApi api)
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        var controller = new AccountController(api)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NullTempDataProvider()),
            Url = new UrlHelper(new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()))
        };
        return controller;
    }

    private class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    [Fact]
    public async Task Login_AsOrganiser_StoresSessionAndGoesToDashboard()
    {
        var api = new FakeApi
        {
            NextResponse = StubHandler.Json(HttpStatusCode.OK,
                "{\"userId\":7,\"firstName\":\"Thandiwe\",\"lastName\":\"Mokoena\",\"role\":\"Organiser\"}")
        };
        var controller = ControllerWith(api);

        var result = await controller.Login(new LoginViewModel { Email = "t@raceday.co.za", Password = "Pass123!" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirect.ControllerName);
        var session = controller.HttpContext.Session;
        Assert.Equal(7, session.GetInt32(SessionKeys.UserId));
        Assert.Equal("Organiser", session.GetString(SessionKeys.Role));
        Assert.Equal("Thandiwe Mokoena", session.GetString(SessionKeys.FullName));
    }

    [Fact]
    public async Task Login_AsParticipant_GoesToBrowseEvents()
    {
        var api = new FakeApi
        {
            NextResponse = StubHandler.Json(HttpStatusCode.OK,
                "{\"userId\":8,\"firstName\":\"Sipho\",\"lastName\":\"Dlamini\",\"role\":\"Participant\"}")
        };

        var result = await ControllerWith(api).Login(new LoginViewModel { Email = "s@raceday.co.za", Password = "Pass123!" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Events", redirect.ControllerName);
        Assert.Equal("Browse", redirect.ActionName);
    }

    [Fact]
    public async Task Login_WithWrongPassword_StaysOnTheFormWithAnError()
    {
        var api = new FakeApi { NextResponse = StubHandler.Json(HttpStatusCode.Unauthorized, "\"Invalid email or password.\"") };
        var controller = ControllerWith(api);

        var result = await controller.Login(new LoginViewModel { Email = "s@raceday.co.za", Password = "wrong" });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Register_WhenTheEmailIsTaken_ShowsTheApiMessage()
    {
        var api = new FakeApi { NextResponse = StubHandler.Json(HttpStatusCode.Conflict, "\"That email address is already registered.\"") };
        var controller = ControllerWith(api);

        var result = await controller.Register(new RegisterViewModel
        {
            FirstName = "Sipho", LastName = "Dlamini", Email = "s@raceday.co.za",
            Password = "Pass123!", ConfirmPassword = "Pass123!", Role = "Participant"
        });

        Assert.IsType<ViewResult>(result);
        var error = Assert.Single(controller.ModelState[string.Empty]!.Errors);
        Assert.Equal("That email address is already registered.", error.ErrorMessage);
        Assert.Equal("/api/auth/register", api.LastEndpoint);
    }

    [Fact]
    public async Task Logout_ClearsTheSession()
    {
        var controller = ControllerWith(new FakeApi());
        controller.HttpContext.Session.SetString(SessionKeys.Role, "Participant");
        controller.HttpContext.Session.SetString(SessionKeys.ApiCookie, ".AspNetCore.Session=x");

        await controller.Logout();

        Assert.Empty(controller.HttpContext.Session.Keys);
    }
}
