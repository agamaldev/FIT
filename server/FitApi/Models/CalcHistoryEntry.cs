using System.Text.Json;

namespace FitApi.Models;

public class CalcHistoryEntry
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public JsonDocument Data { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public ApplicationUser? User { get; set; }
}
