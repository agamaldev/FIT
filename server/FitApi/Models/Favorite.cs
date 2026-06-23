using System.Text.Json;

namespace FitApi.Models;

public class Favorite
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public JsonDocument Data { get; set; } = default!;
    public DateTimeOffset AddedAt { get; set; }
    public ApplicationUser? User { get; set; }
}
