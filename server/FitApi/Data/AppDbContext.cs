using System.Text.Json;
using FitApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

    // SQLite has no jsonb type and can't map JsonDocument natively — store the
    // payload as TEXT and (de)serialize it. Postgres keeps the native jsonb mapping.
    private static readonly ValueConverter<JsonDocument, string> JsonToText =
        new(v => v.RootElement.GetRawText(),
            v => JsonDocument.Parse(v, default));

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var isSqlite = Database.IsSqlite();

        builder.Entity<Favorite>(e =>
        {
            if (isSqlite) e.Property(x => x.Data).HasConversion(JsonToText).HasColumnType("TEXT");
            else e.Property(x => x.Data).HasColumnType("jsonb");
            e.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NutritionPlan>(e =>
        {
            if (isSqlite) e.Property(x => x.Data).HasConversion(JsonToText).HasColumnType("TEXT");
            else e.Property(x => x.Data).HasColumnType("jsonb");
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
            if (isSqlite) e.Property(x => x.Data).HasConversion(JsonToText).HasColumnType("TEXT");
            else e.Property(x => x.Data).HasColumnType("jsonb");
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
