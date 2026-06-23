namespace FitApi.Models;

public class CompletedDate
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public DateOnly Date { get; set; }
    public ApplicationUser? User { get; set; }
}
