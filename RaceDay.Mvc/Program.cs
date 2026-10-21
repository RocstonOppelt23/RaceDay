using System.Globalization;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

// One fixed culture so "21.1" km and dd/MM/yyyy dates behave the same on every
// machine and inside Docker (some regional settings use a comma for decimals).
var appCulture = new CultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentCulture = appCulture;
CultureInfo.DefaultThreadCurrentUICulture = appCulture;

// MVC, with one filter that turns "API is down" and "API session expired"
// into friendly pages instead of raw error screens.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});

// One named HttpClient for every call to the Part 2 API.
// Cookies are handled by ApiClient itself (UseCookies = false), so each MVC user
// carries their OWN API session cookie rather than sharing one cookie jar.
builder.Services.AddHttpClient("RaceDayApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler { UseCookies = false };

    // Only for local Docker testing: lets the container trust the API's
    // self-signed development certificate. Off unless explicitly switched on.
    if (builder.Configuration.GetValue<bool>("ApiAcceptAnyCertificate"))
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    return handler;
});

// Session state: holds UserId, Role, FullName and the API session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IApiClient, ApiClient>();

var app = builder.Build();

// Friendly error page in every environment: no raw stack traces for users.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();          // must come before UseAuthorization and the routes
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
