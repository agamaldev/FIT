using Microsoft.AspNetCore.Identity;

namespace FitApi.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public string? Goal { get; set; }
    public string? Activity { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
