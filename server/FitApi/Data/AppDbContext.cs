using FitApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();
    public DbSet<CustomPlanItem> CustomPlanItems => Set<CustomPlanItem>();
    public DbSet<CompletedDate> CompletedDates => Set<CompletedDate>();
    public DbSet<CalcHistoryEntry> CalcHistory => Set<CalcHistoryEntry>();
    public DbSet<WeightEntry> WeightLog => Set<WeightEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Favorite>(e =>
        {
            e.Property(x => x.Data).HasColumnType("jsonb");
            e.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NutritionPlan>(e =>
        {
            e.Property(x => x.Data).HasColumnType("jsonb");
            e.HasIndex(x => new { x.UserId, x.PlanId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CustomPlanItem>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CompletedDate>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CalcHistoryEntry>(e =>
        {
            e.Property(x => x.Data).HasColumnType("jsonb");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WeightEntry>(e =>
        {
            e.Property(x => x.Weight).HasColumnType("numeric");
            e.Property(x => x.Waist).HasColumnType("numeric");
            e.Property(x => x.Chest).HasColumnType("numeric");
            e.Property(x => x.Arms).HasColumnType("numeric");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.Weight).HasColumnType("numeric");
            e.Property(x => x.Height).HasColumnType("numeric");
        });
    }
}
