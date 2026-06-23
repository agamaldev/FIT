using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class FavoritesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FavoritesTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static JsonElement Obj(object value) =>
        JsonSerializer.SerializeToElement(value);

    [Fact]
    public async Task Toggle_Add_Then_Get_Returns_Stored_Item_With_Type_And_AddedAt()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "fav-add@test.com", "pass1234", "FavAdd");

        var post = await client.PostAsJsonAsync("/api/favorites",
            Obj(new { id = "ex-1", name = "Bench Press" }));
        post.StatusCode.Should().Be(HttpStatusCode.OK);

        using var postDoc = JsonDocument.Parse(await post.Content.ReadAsStringAsync());
        postDoc.RootElement.GetProperty("favorited").GetBoolean().Should().BeTrue();

        var get = await client.GetAsync("/api/favorites");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        getDoc.RootElement.GetArrayLength().Should().Be(1);

        var item = getDoc.RootElement[0];
        item.GetProperty("id").GetString().Should().Be("ex-1");
        item.GetProperty("name").GetString().Should().Be("Bench Press");
        item.GetProperty("type").GetString().Should().Be("exercise");
        item.TryGetProperty("addedAt", out var addedAt).Should().BeTrue();
        addedAt.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Toggle_Add_Honors_Explicit_Type()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "fav-type@test.com", "pass1234", "FavType");

        await client.PostAsJsonAsync("/api/favorites",
            Obj(new { id = "ex-typed", type = "cardio" }));

        var get = await client.GetAsync("/api/favorites");
        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.GetArrayLength().Should().Be(1);
        getDoc.RootElement[0].GetProperty("type").GetString().Should().Be("cardio");
    }

    [Fact]
    public async Task Toggle_Same_Id_Twice_Removes_It()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "fav-toggle@test.com", "pass1234", "FavToggle");

        var add = await client.PostAsJsonAsync("/api/favorites", Obj(new { id = "ex-2" }));
        using (var addDoc = JsonDocument.Parse(await add.Content.ReadAsStringAsync()))
            addDoc.RootElement.GetProperty("favorited").GetBoolean().Should().BeTrue();

        var remove = await client.PostAsJsonAsync("/api/favorites", Obj(new { id = "ex-2" }));
        remove.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var remDoc = JsonDocument.Parse(await remove.Content.ReadAsStringAsync()))
            remDoc.RootElement.GetProperty("favorited").GetBoolean().Should().BeFalse();

        var get = await client.GetAsync("/api/favorites");
        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Delete_By_ItemId_Removes_It()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "fav-del@test.com", "pass1234", "FavDel");

        await client.PostAsJsonAsync("/api/favorites", Obj(new { id = "ex-3" }));

        var del = await client.DeleteAsync("/api/favorites/ex-3");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await client.GetAsync("/api/favorites");
        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_Requires_Authentication()
    {
        var anon = _factory.CreateClient();
        var get = await anon.GetAsync("/api/favorites");
        get.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Owner_Isolation_UserB_Cannot_See_Or_Delete_UserA_Favorite()
    {
        var userA = await _factory.RegisterAndLoginAsync(
            "fav-a@test.com", "pass1234", "UserA");
        var userB = await _factory.RegisterAndLoginAsync(
            "fav-b@test.com", "pass1234", "UserB");

        await userA.PostAsJsonAsync("/api/favorites", Obj(new { id = "shared-id" }));

        // B sees an empty list (cannot read A's row).
        var bGet = await userB.GetAsync("/api/favorites");
        using (var bDoc = JsonDocument.Parse(await bGet.Content.ReadAsStringAsync()))
            bDoc.RootElement.GetArrayLength().Should().Be(0);

        // B's DELETE for the same ItemId is a no-op-200 and does NOT remove A's row.
        var bDel = await userB.DeleteAsync("/api/favorites/shared-id");
        bDel.StatusCode.Should().Be(HttpStatusCode.OK);

        var aGet = await userA.GetAsync("/api/favorites");
        using var aDoc = JsonDocument.Parse(await aGet.Content.ReadAsStringAsync());
        aDoc.RootElement.GetArrayLength().Should().Be(1);
        aDoc.RootElement[0].GetProperty("id").GetString().Should().Be("shared-id");
    }
}
