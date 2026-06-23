using System.Text.Json;
using System.Text.Json.Nodes;
using FitApi.Data;
using FitApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Controllers;

[Authorize]
[ApiController]
[Route("api/calc-history")]
public class CalcHistoryController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public CalcHistoryController(AppDbContext db) => _db = db;

    // GET api/calc-history -> array of stored Data objects, each with an added "id":<dbid>, newest first
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var rows = await _db.CalcHistory
            .Where(e => e.UserId == UserId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.Data })
            .ToListAsync();

        var result = new JsonArray();
        foreach (var row in rows)
        {
            // Re-parse the stored jsonb into a mutable JsonObject and inject the DB id.
            var obj = JsonNode.Parse(row.Data.RootElement.GetRawText()) as JsonObject ?? new JsonObject();
            obj["id"] = row.Id;
            result.Add(obj);
        }

        return Ok(result);
    }

    // POST api/calc-history -> stores the posted JSON as jsonb + CreatedAt; returns { id }
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] JsonElement result)
    {
        var entry = new CalcHistoryEntry
        {
            UserId = UserId,
            Data = JsonDocument.Parse(result.GetRawText()),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.CalcHistory.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new { id = entry.Id });
    }

    // DELETE api/calc-history/{id} -> deletes only the current user's row; no-op otherwise
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var entry = await _db.CalcHistory
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == UserId);

        if (entry is not null)
        {
            _db.CalcHistory.Remove(entry);
            await _db.SaveChangesAsync();
        }

        return Ok(new { });
    }
}
