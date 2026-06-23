namespace FitApi.Models;

public class CustomPlanItem
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Tab { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int OrderIndex { get; set; }
    public ApplicationUser? User { get; set; }
}
