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
[Route("api/favorites")]
public class FavoritesController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public FavoritesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rows = await _db.Favorites
            .Where(f => f.UserId == UserId)
            .OrderBy(f => f.Id)
            .Select(f => f.Data)
            .ToListAsync();

        var items = rows.Select(d => d.RootElement).ToList();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle([FromBody] JsonElement fav)
    {
        if (!fav.TryGetProperty("id", out var idElement))
        {
            return Problem(
                statusCode: 400,
                title: "bad-favorite",
                extensions: new Dictionary<string, object?> { ["code"] = "bad-favorite" });
        }

        var itemId = idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString()
            : idElement.GetRawText();

        if (string.IsNullOrEmpty(itemId))
        {
            return Problem(
                statusCode: 400,
                title: "bad-favorite",
                extensions: new Dictionary<string, object?> { ["code"] = "bad-favorite" });
        }

        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == UserId && f.ItemId == itemId);

        if (existing is not null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { favorited = false });
        }

        var node = new JsonObject();
        foreach (var prop in fav.EnumerateObject())
        {
            node[prop.Name] = JsonNode.Parse(prop.Value.GetRawText());
        }

        string type = "exercise";
        if (fav.TryGetProperty("type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(typeElement.GetString()))
        {
            type = typeElement.GetString()!;
        }
        node["type"] = type;
        node["addedAt"] = DateTimeOffset.UtcNow.ToString("O");

        var data = JsonSerializer.SerializeToDocument(node);

        _db.Favorites.Add(new Favorite
        {
            UserId = UserId,
            ItemId = itemId,
            Type = type,
            Data = data,
            AddedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        return Ok(new { favorited = true });
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> Delete(string itemId)
    {
        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == UserId && f.ItemId == itemId);

        if (existing is not null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync();
        }

        return Ok(new { });
    }
}
