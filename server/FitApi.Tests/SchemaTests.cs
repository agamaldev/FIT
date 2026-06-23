using System.Text.Json;
using FitApi.Data;
using FitApi.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitApi.Tests;

public class SchemaTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SchemaTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // Force the host (and thus startup migration) to run.
        _ = _factory.Server;
    }

    [Fact]
    public async Task DuplicateFavorite_SameUserAndItem_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new ApplicationUser
        {
            UserName = "schema-user@test.com",
            Email = "schema-user@test.com"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Favorites.Add(new Favorite
        {
            UserId = user.Id,
            ItemId = "ex-1",
            Type = "exercise",
            Data = JsonDocument.Parse("""{"id":"ex-1"}"""),
            AddedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        db.Favorites.Add(new Favorite
        {
            UserId = user.Id,
            ItemId = "ex-1",
            Type = "exercise",
            Data = JsonDocument.Parse("""{"id":"ex-1"}"""),
            AddedAt = DateTimeOffset.UtcNow
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
