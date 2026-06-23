using FitApi.Dtos;
using FitApi.Services;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new DateOnly(2026, 6, 23);

    [Fact]
    public void Empty_ReturnsAllZero()
    {
        var result = StreakCalculator.Compute(Array.Empty<DateOnly>(), Today);

        result.Should().Be(new StreakStats(0, 0, 0));
    }

    [Fact]
    public void TodayPlusYesterdayPlusTwoDaysAgo_AllConsecutive_Streak3Longest3Total3()
    {
        var dates = new[]
        {
            Today,
            Today.AddDays(-1),
            Today.AddDays(-2),
        };

        var result = StreakCalculator.Compute(dates, Today);

        result.Should().Be(new StreakStats(3, 3, 3));
    }

    [Fact]
    public void GapBeforeToday_CurrentStreakShorter_LongestReflectsBestRun()
    {
        // Best run is the 4-day block 10 days ago; current run (ending today) is only 2 days.
        // total = 6 distinct days.
        var dates = new[]
        {
            Today,                  // current run
            Today.AddDays(-1),      // current run
            // gap at -2, -3
            Today.AddDays(-7),      // best run
            Today.AddDays(-8),      // best run
            Today.AddDays(-9),      // best run
            Today.AddDays(-10),     // best run
        };

        var result = StreakCalculator.Compute(dates, Today);

        result.Should().Be(new StreakStats(2, 4, 6));
    }

    [Fact]
    public void LatestIsYesterday_CurrentStreakContinues()
    {
        var dates = new[]
        {
            Today.AddDays(-1),
            Today.AddDays(-2),
        };

        var result = StreakCalculator.Compute(dates, Today);

        result.Should().Be(new StreakStats(2, 2, 2));
    }

    [Fact]
    public void LatestIsTwoDaysAgo_CurrentStreakIsZero_LongestAndTotalIntact()
    {
        var dates = new[]
        {
            Today.AddDays(-2),
            Today.AddDays(-3),
        };

        var result = StreakCalculator.Compute(dates, Today);

        result.Should().Be(new StreakStats(0, 2, 2));
    }

    [Fact]
    public void DuplicateDates_AreDistinctCounted()
    {
        var dates = new[]
        {
            Today,
            Today,
            Today.AddDays(-1),
        };

        var result = StreakCalculator.Compute(dates, Today);

        result.Should().Be(new StreakStats(2, 2, 2));
    }
}
