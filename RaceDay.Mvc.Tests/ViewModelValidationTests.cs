using System.ComponentModel.DataAnnotations;
using RaceDay.Mvc.ViewModels;
using Xunit;

namespace RaceDay.Mvc.Tests;

public class ViewModelValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static EventFormViewModel ValidEvent() => new()
    {
        Name = "Two Oceans Half",
        EventDate = DateTime.Today.AddDays(20).AddHours(6),
        Location = "Newlands",
        Distance = 21.1m,
        EventType = "Run"
    };

    [Fact]
    public void ValidEvent_PassesValidation() => Assert.Empty(Validate(ValidEvent()));

    [Fact]
    public void NewEventInThePast_IsRejected()
    {
        var model = ValidEvent();
        model.EventDate = DateTime.Today.AddDays(-3);

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(EventFormViewModel.EventDate)));
    }

    [Fact]
    public void EditingAPastEvent_IsAllowed()
    {
        var model = ValidEvent();
        model.EventId = 5;
        model.EventDate = DateTime.Today.AddDays(-3);

        Assert.Empty(Validate(model));
    }

    [Theory]
    [InlineData("Swim")]
    [InlineData("")]
    public void UnknownEventType_IsRejected(string type)
    {
        var model = ValidEvent();
        model.EventType = type;

        Assert.NotEmpty(Validate(model));
    }

    [Fact]
    public void ZeroDistance_IsRejected()
    {
        var model = ValidEvent();
        model.Distance = 0;

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(EventFormViewModel.Distance)));
    }

    [Fact]
    public void Register_MismatchedPasswords_AreRejected()
    {
        var model = new RegisterViewModel
        {
            FirstName = "Sipho", LastName = "Dlamini", Email = "s@raceday.co.za",
            Password = "Pass123!", ConfirmPassword = "Pass124!", Role = "Participant"
        };

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }

    [Theory]
    [InlineData("00:52:35", true)]
    [InlineData("1:42:10", true)]
    [InlineData("52:35", false)]
    [InlineData("00:61:00", false)]
    public void FinishTime_MustBeHoursMinutesSeconds(string time, bool valid)
    {
        var model = new ResultFormViewModel { FinishTime = time, FinishingPosition = 1 };

        Assert.Equal(valid, !Validate(model).Any());
    }
}
