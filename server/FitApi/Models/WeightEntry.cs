namespace FitApi.Models;

public class WeightEntry
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public DateOnly Date { get; set; }
    public decimal Weight { get; set; }
    public decimal? Waist { get; set; }
    public decimal? Chest { get; set; }
    public decimal? Arms { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ApplicationUser? User { get; set; }
}
