using FitApi.Data;
using FitApi.Dtos;
using FitApi.Models;
using FitApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Controllers;

[ApiController]
[Authorize]
[Route("api/workouts")]
public class WorkoutsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public WorkoutsController(AppDbContext db) => _db = db;

    [HttpGet("completed")]
    public async Task<ActionResult<string[]>> GetCompleted()
    {
        var dates = await _db.CompletedDates
            .Where(c => c.UserId == UserId)
            .OrderByDescending(c => c.Date)
            .Select(c => c.Date)
            .ToListAsync();

        var result = dates
            .Select(d => d.ToString("yyyy-MM-dd"))
            .ToArray();

        return Ok(result);
    }

    [HttpPost("complete")]
    public async Task<ActionResult<StreakStats>> Complete()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var exists = await _db.CompletedDates
            .AnyAsync(c => c.UserId == UserId && c.Date == today);

        if (!exists)
        {
            _db.CompletedDates.Add(new CompletedDate
            {
                UserId = UserId,
                Date = today,
            });
            await _db.SaveChangesAsync();
        }

        return Ok(await ComputeStatsAsync(today));
    }

    [HttpDelete("complete")]
    public async Task<ActionResult<StreakStats>> Uncomplete()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var entry = await _db.CompletedDates
            .FirstOrDefaultAsync(c => c.UserId == UserId && c.Date == today);

        if (entry is not null)
        {
            _db.CompletedDates.Remove(entry);
            await _db.SaveChangesAsync();
        }

        return Ok(await ComputeStatsAsync(today));
    }

    private async Task<StreakStats> ComputeStatsAsync(DateOnly today)
    {
        var dates = await _db.CompletedDates
            .Where(c => c.UserId == UserId)
            .Select(c => c.Date)
            .ToListAsync();

        return StreakCalculator.Compute(dates, today);
    }
}
