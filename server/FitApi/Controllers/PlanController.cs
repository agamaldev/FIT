using FitApi.Data;
using FitApi.Dtos;
using FitApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Controllers;

[ApiController]
[Authorize]
[Route("api/plan")]
public class PlanController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public PlanController(AppDbContext db) => _db = db;

    private static PlanItemDto ToDto(CustomPlanItem i) =>
        new(i.ItemId, i.NameAr, i.NameEn, i.Tab, i.Sets, i.Reps, i.OrderIndex);

    // GET api/plan -> PlanItemDto[] ordered by OrderIndex ascending
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanItemDto>>> Get()
    {
        var items = await _db.CustomPlanItems
            .Where(i => i.UserId == UserId)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();
        return Ok(items.Select(ToDto));
    }

    // POST api/plan -> {added:false} if ItemId exists, else insert with defaults -> {added:true}
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddPlanItemRequest req)
    {
        var exists = await _db.CustomPlanItems
            .AnyAsync(i => i.UserId == UserId && i.ItemId == req.Id);
        if (exists)
            return Ok(new { added = false });

        var count = await _db.CustomPlanItems.CountAsync(i => i.UserId == UserId);

        _db.CustomPlanItems.Add(new CustomPlanItem
        {
            UserId = UserId,
            ItemId = req.Id,
            NameAr = req.NameAr,
            NameEn = req.NameEn,
            Tab = req.Tab,
            Sets = req.Sets ?? 4,
            Reps = req.Reps ?? 10,
            OrderIndex = count
        });
        await _db.SaveChangesAsync();
        return Ok(new { added = true });
    }

    // PUT api/plan -> replace OrderIndex/Sets/Reps for the user's matching items by Id
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] PlanItemDto[] items)
    {
        var mine = await _db.CustomPlanItems
            .Where(i => i.UserId == UserId)
            .ToListAsync();
        var byId = mine.ToDictionary(i => i.ItemId);

        foreach (var dto in items)
        {
            if (byId.TryGetValue(dto.Id, out var existing))
            {
                existing.OrderIndex = dto.Order;
                existing.Sets = dto.Sets;
                existing.Reps = dto.Reps;
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new { });
    }

    // DELETE api/plan/{itemId} -> remove the item, then renumber remaining 0..n
    [HttpDelete("{itemId}")]
    public async Task<IActionResult> Remove(string itemId)
    {
        var toRemove = await _db.CustomPlanItems
            .FirstOrDefaultAsync(i => i.UserId == UserId && i.ItemId == itemId);
        if (toRemove == null)
            return Ok(new { });

        _db.CustomPlanItems.Remove(toRemove);
        await _db.SaveChangesAsync();

        var remaining = await _db.CustomPlanItems
            .Where(i => i.UserId == UserId)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();
        for (var i = 0; i < remaining.Count; i++)
            remaining[i].OrderIndex = i;
        await _db.SaveChangesAsync();

        return Ok(new { });
    }

    // DELETE api/plan -> clear all of the user's items
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var mine = await _db.CustomPlanItems
            .Where(i => i.UserId == UserId)
            .ToListAsync();
        _db.CustomPlanItems.RemoveRange(mine);
        await _db.SaveChangesAsync();
        return Ok(new { });
    }
}
