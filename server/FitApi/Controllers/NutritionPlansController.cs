using System.Text.Json;
using System.Text.Json.Nodes;
using FitApi.Data;
using FitApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Controllers;

[ApiController]
[Authorize]
[Route("api/nutrition-plans")]
public class NutritionPlansController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public NutritionPlansController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rows = await _db.NutritionPlans
            .Where(p => p.UserId == UserId)
            .OrderBy(p => p.SavedAt)
            .Select(p => p.Data)
            .ToListAsync();

        // Each Data JsonDocument already contains id + savedAt; emit as a raw array.
        var items = rows.Select(d => d.RootElement).ToList();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] JsonElement plan)
    {
        var planId = ComputePlanId(plan);

        var exists = await _db.NutritionPlans
            .AnyAsync(p => p.UserId == UserId && p.PlanId == planId);
        if (exists)
        {
            return Ok(new { saved = false });
        }

        // Build stored payload = {...plan, id: planId, savedAt: nowIso}.
        var obj = JsonNode.Parse(plan.GetRawText())!.AsObject();
        obj["id"] = planId;
        var now = DateTimeOffset.UtcNow;
        obj["savedAt"] = now.ToString("O");

        var entity = new NutritionPlan
        {
            UserId = UserId,
            PlanId = planId,
            CalId = ReadString(plan, "calId"),
            VarId = ReadString(plan, "varId"),
            Data = JsonDocument.Parse(obj.ToJsonString()),
            SavedAt = now
        };

        _db.NutritionPlans.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new { saved = true });
    }

    [HttpDelete("{planId}")]
    public async Task<IActionResult> Delete(string planId)
    {
        var row = await _db.NutritionPlans
            .FirstOrDefaultAsync(p => p.UserId == UserId && p.PlanId == planId);
        if (row is not null)
        {
            _db.NutritionPlans.Remove(row);
            await _db.SaveChangesAsync();
        }

        return Ok(new { });
    }

    private static string ComputePlanId(JsonElement plan)
    {
        var id = ReadString(plan, "id");
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        var calId = ReadString(plan, "calId") ?? "";
        var varId = ReadString(plan, "varId") ?? "";
        return $"{calId}-{varId}";
    }

    private static string? ReadString(JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(name, out var v) &&
            v.ValueKind == JsonValueKind.String)
        {
            return v.GetString();
        }
        return null;
    }
}
