using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

/// <summary>
/// Shared setup for every test class: a fresh in-memory database and a
/// controller wired to a fake session. No SQL Server needed, so the same
/// tests run locally and in GitHub Actions.
/// </summary>
public static class TestHelper
{
    /// <summary>Creates an empty database unique to one test.</summary>
    public static AppDbContext NewContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());

        // Ensure EF Core internal services are available to the context by
        // creating a dedicated service provider for the in-memory provider.
        var serviceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        optionsBuilder.UseInternalServiceProvider(serviceProvider);

        var context = new AppDbContext(optionsBuilder.Options);
        // Force model initialization and ensure the in-memory database is created
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>Attaches a fake session to a controller and logs the given user in.</summary>
    public static void SignIn(ControllerBase controller, int userId, string role)
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        httpContext.Session.SetInt32("UserId", userId);
        httpContext.Session.SetString("Role", role);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    /// <summary>Attaches a fake session with nobody logged in.</summary>
    public static void SignedOut(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = new TestSession() }
        };
    }

    /// <summary>Adds a user with a real BCrypt hash of "Pass123!".</summary>
    public static User AddUser(AppDbContext context, string firstName, string email, string role)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = "Tester",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            Role = role,
            Profile = new UserProfile()
        };

        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    /// <summary>Seeds an organiser, a participant, one event and one category.</summary>
    public static (User organiser, User participant, Event ev, Category category) Seed(AppDbContext context)
    {
        var organiser = AddUser(context, "Thandiwe", "organiser@raceday.co.za", "Organiser");
        var participant = AddUser(context, "Sipho", "participant@raceday.co.za", "Participant");

        var ev = new Event
        {
            OrganiserId = organiser.UserId,
            Name = "Cape Town Road Run",
            Description = "A road running event through Cape Town.",
            EventDate = new DateTime(2027, 3, 14, 7, 0, 0),
            Location = "Cape Town",
            Distance = 10.00m,
            EventType = "Run"
        };
        context.Events.Add(ev);
        context.SaveChanges();

        var category = new Category
        {
            EventId = ev.EventId,
            Name = "Open 10km",
            Description = "10 kilometre open category."
        };
        context.Categories.Add(category);
        context.SaveChanges();

        return (organiser, participant, ev, category);
    }

    /// <summary>Adds an active enrolment.</summary>
    public static Enrolment AddEnrolment(AppDbContext context, int participantId, int eventId, int categoryId)
    {
        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = eventId,
            CategoryId = categoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Active"
        };

        context.Enrolments.Add(enrolment);
        context.SaveChanges();
        return enrolment;
    }
}
