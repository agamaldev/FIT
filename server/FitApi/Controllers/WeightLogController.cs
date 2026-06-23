using System.Globalization;
using FitApi.Data;
using FitApi.Dtos;
using FitApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Controllers;

[ApiController]
[Route("api/weight-log")]
[Authorize]
public class WeightLogController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public WeightLogController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WeightEntryDto>>> Get()
    {
        var entries = await _db.WeightLog
            .Where(w => w.UserId == UserId)
            .OrderByDescending(w => w.Date)
            .Select(w => new WeightEntryDto(
                w.Id,
                w.Date.ToString("yyyy-MM-dd"),
                w.Weight,
                w.Waist,
                w.Chest,
                w.Arms))
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<WeightEntryDto>> Post([FromBody] WeightEntryRequest req)
    {
        if (req.Weight < 10m || req.Weight > 500m)
        {
            return Problem(
                statusCode: 400,
                title: "bad-weight",
                extensions: new Dictionary<string, object?> { ["code"] = "bad-weight" });
        }

        DateOnly date;
        if (string.IsNullOrWhiteSpace(req.Date))
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        else if (!DateOnly.TryParseExact(req.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var entry = new WeightEntry
        {
            UserId = UserId,
            Date = date,
            Weight = req.Weight,
            Waist = req.Waist,
            Chest = req.Chest,
            Arms = req.Arms,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.WeightLog.Add(entry);
        await _db.SaveChangesAsync();

        var dto = new WeightEntryDto(
            entry.Id,
            entry.Date.ToString("yyyy-MM-dd"),
            entry.Weight,
            entry.Waist,
            entry.Chest,
            entry.Arms);

        return Created($"/api/weight-log/{entry.Id}", dto);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var entry = await _db.WeightLog
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == UserId);

        if (entry is not null)
        {
            _db.WeightLog.Remove(entry);
            await _db.SaveChangesAsync();
        }

        return Ok(new { });
    }
}
