using System.Text.Json;

namespace FitApi.Models;

public class NutritionPlan
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public string PlanId { get; set; } = default!;
    public string? CalId { get; set; }
    public string? VarId { get; set; }
    public JsonDocument Data { get; set; } = default!;
    public DateTimeOffset SavedAt { get; set; }
    public ApplicationUser? User { get; set; }
}
