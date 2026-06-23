using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class PlanTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlanTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static object AddBody(string id, string? nameAr = null, string? nameEn = null,
        string? tab = null, int? sets = null, int? reps = null) =>
        new { id, nameAr, nameEn, tab, sets, reps };

    [Fact]
    public async Task Add_Then_Get_Returns_Item_At_Order_Zero()
    {
        var client = await _factory.RegisterAndLoginAsync("plan-add@example.com", "Passw0rd!");

        var add = await client.PostAsJsonAsync("/api/plan",
            AddBody("ex-1", nameAr: "ضغط", nameEn: "Bench", tab: "chest", sets: 5, reps: 8));
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var addDoc = JsonDocument.Parse(await add.Content.ReadAsStringAsync());
        addDoc.RootElement.GetProperty("added").GetBoolean().Should().BeTrue();

        var get = await client.GetAsync("/api/plan");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = JsonDocument.Parse(await get.Content.ReadAsStringAsync()).RootElement;
        items.GetArrayLength().Should().Be(1);
        var item = items[0];
        item.GetProperty("id").GetString().Should().Be("ex-1");
        item.GetProperty("nameAr").GetString().Should().Be("ضغط");
        item.GetProperty("nameEn").GetString().Should().Be("Bench");
        item.GetProperty("tab").GetString().Should().Be("chest");
        item.GetProperty("sets").GetInt32().Should().Be(5);
        item.GetProperty("reps").GetInt32().Should().Be(8);
        item.GetProperty("order").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Add_Applies_Default_Sets_And_Reps_When_Null()
    {
        var client = await _factory.RegisterAndLoginAsync("plan-defaults@example.com", "Passw0rd!");

        var add = await client.PostAsJsonAsync("/api/plan", AddBody("ex-def"));
        add.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("sets").GetInt32().Should().Be(4);
        items[0].GetProperty("reps").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task Add_Duplicate_Returns_Added_False_And_Does_Not_Duplicate()
    {
        var client = await _factory.RegisterAndLoginAsync("plan-dup@example.com", "Passw0rd!");

        (await client.PostAsJsonAsync("/api/plan", AddBody("ex-dup"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var dup = await client.PostAsJsonAsync("/api/plan", AddBody("ex-dup"));
        dup.StatusCode.Should().Be(HttpStatusCode.OK);
        var dupDoc = JsonDocument.Parse(await dup.Content.ReadAsStringAsync());
        dupDoc.RootElement.GetProperty("added").GetBoolean().Should().BeFalse();

        var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
        items.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Put_Reorder_Is_Reflected_In_Get()
    {
        var client = await _factory.RegisterAndLoginAsync("plan-reorder@example.com", "Passw0rd!");

        await client.PostAsJsonAsync("/api/plan", AddBody("a"));
        await client.PostAsJsonAsync("/api/plan", AddBody("b"));
        await client.PostAsJsonAsync("/api/plan", AddBody("c"));

        // Reverse order, and change sets/reps on "a".
        var payload = new[]
        {
            new { id = "c", nameAr = (string?)null, nameEn = (string?)null, tab = (string?)null, sets = 4, reps = 10, order = 0 },
            new { id = "b", nameAr = (string?)null, nameEn = (string?)null, tab = (string?)null, sets = 4, reps = 10, order = 1 },
            new { id = "a", nameAr = (string?)null, nameEn = (string?)null, tab = (string?)null, sets = 3, reps = 15, order = 2 },
        };
        var put = await client.PutAsJsonAsync("/api/plan", payload);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
        items.GetArrayLength().Should().Be(3);
        items[0].GetProperty("id").GetString().Should().Be("c");
        items[1].GetProperty("id").GetString().Should().Be("b");
        items[2].GetProperty("id").GetString().Should().Be("a");
        items[2].GetProperty("order").GetInt32().Should().Be(2);
        items[2].GetProperty("sets").GetInt32().Should().Be(3);
        items[2].GetProperty("reps").GetInt32().Should().Be(15);
    }

    [Fact]
    public async Task Delete_Item_Renumbers_Remaining_From_Zero()
    {
        var client = await _factory.RegisterAndLoginAsync("plan-delete@example.com", "Passw0rd!");

        await client.PostAsJsonAsync("/api/plan", AddBody("a")); // order 0
        await client.PostAsJsonAsync("/api/plan", AddBody("b")); // order 1
        await client.PostAsJsonAsync("/api/plan", AddBody("c")); // order 2

        var del = await client.DeleteAsync("/api/plan/b");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
        items.GetArrayLength().Should().Be(2);
        items[0].GetProperty("id").GetString().Should().Be("a");
        items[0].GetProperty("order").GetInt32().Should().Be(0);
        items[1].GetProperty("id").GetString().Should().Be("c");
        items[1].GetProperty("order").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Clear_Empties_The_Plan()
    {
        var client = await _factory.RegisterAndLoginAsync("plan-clear@example.com", "Passw0rd!");

        await client.PostAsJsonAsync("/api/plan", AddBody("a"));
        await client.PostAsJsonAsync("/api/plan", AddBody("b"));

        var clear = await client.DeleteAsync("/api/plan");
        clear.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
        items.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Owner_Isolation_User_B_Cannot_See_Or_Delete_User_A_Items()
    {
        var a = await _factory.RegisterAndLoginAsync("plan-owner-a@example.com", "Passw0rd!");
        var b = await _factory.RegisterAndLoginAsync("plan-owner-b@example.com", "Passw0rd!");

        (await a.PostAsJsonAsync("/api/plan", AddBody("secret"))).StatusCode.Should().Be(HttpStatusCode.OK);

        // B sees nothing.
        var bItems = JsonDocument.Parse(await (await b.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
        bItems.GetArrayLength().Should().Be(0);

        // B's delete of A's item id is a no-op for A.
        (await b.DeleteAsync("/api/plan/secret")).StatusCode.Should().Be(HttpStatusCode.OK);

        var aItems = JsonDocument.Parse(await (await a.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
        aItems.GetArrayLength().Should().Be(1);
        aItems[0].GetProperty("id").GetString().Should().Be("secret");
    }

    [Fact]
    public async Task Anonymous_Get_Is_Unauthorized()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/plan");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
