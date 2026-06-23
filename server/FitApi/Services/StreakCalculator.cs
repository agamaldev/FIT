using FitApi.Dtos;

namespace FitApi.Services;

// Faithful port of the JS _getStreakStats in assets/js/auth.js.
// total   = count of distinct days.
// longest = longest run of consecutive calendar days.
// streak  = current run ending at the latest date, but ONLY counted when the
//           latest date is today or yesterday; otherwise 0.
public static class StreakCalculator
{
    public static StreakStats Compute(IEnumerable<DateOnly> dates, DateOnly today)
    {
        var nums = (dates ?? Array.Empty<DateOnly>())
            .Distinct()
            .Select(d => d.DayNumber)
            .OrderBy(n => n)
            .ToArray();

        var total = nums.Length;
        if (total == 0)
        {
            return new StreakStats(0, 0, 0);
        }

        var longest = 1;
        var run = 1;
        for (var i = 1; i < nums.Length; i++)
        {
            if (nums[i] == nums[i - 1] + 1)
            {
                run++;
                if (run > longest)
                {
                    longest = run;
                }
            }
            else
            {
                run = 1;
            }
        }

        var todayNum = today.DayNumber;
        var last = nums[^1];
        var current = 0;
        if (last == todayNum || last == todayNum - 1)
        {
            current = 1;
            for (var i = nums.Length - 1; i > 0; i--)
            {
                if (nums[i] == nums[i - 1] + 1)
                {
                    current++;
                }
                else
                {
                    break;
                }
            }
        }

        return new StreakStats(current, longest, total);
    }
}
