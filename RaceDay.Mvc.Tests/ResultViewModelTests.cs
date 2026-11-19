using RaceDay.Mvc.ViewModels;
using Xunit;

namespace RaceDay.Mvc.Tests;

public class ResultViewModelTests
{
    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(47, "47th")]
    [InlineData(112, "112th")]
    public void Ordinal_UsesTheRightSuffix(int position, string expected)
    {
        Assert.Equal(expected, ResultViewModel.Ordinal(position));
    }

    [Fact]
    public void PositionText_ShowsPositionOutOfTotalFinishers()
    {
        var result = new ResultViewModel { FinishingPosition = 47, TotalFinishers = 312 };

        Assert.Equal("47th out of 312", result.PositionText);
    }
}
