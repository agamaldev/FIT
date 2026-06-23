using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class CalcHistoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CalcHistoryTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement;

    [Fact]
    public async Task Post_StoresResult_AndReturnsId()
    {
        var client = await _factory.RegisterAndLoginAsync("calc-add@test.com", "password1", "Calc Add");

        var resp = await client.PostAsJsonAsync("/api/calc-history",
            Json("""{"calories":2000,"bmr":1500,"goal":"cut"}"""));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("id", out var idProp).Should().BeTrue();
        idProp.GetInt64().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_ReturnsStoredData_WithId_NewestFirst()
    {
        var client = await _factory.RegisterAndLoginAsync("calc-list@test.com", "password1", "Calc List");

        var first = await client.PostAsJsonAsync("/api/calc-history", Json("""{"calories":1000,"label":"first"}"""));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstId = JsonDocument.Parse(await first.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();

        var second = await client.PostAsJsonAsync("/api/calc-history", Json("""{"calories":2000,"label":"second"}"""));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondId = JsonDocument.Parse(await second.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();

        var listResp = await client.GetAsync("/api/calc-history");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;

        arr.ValueKind.Should().Be(JsonValueKind.Array);
        arr.GetArrayLength().Should().Be(2);

        // newest (second) first
        arr[0].GetProperty("id").GetInt64().Should().Be(secondId);
        arr[0].GetProperty("label").GetString().Should().Be("second");
        arr[0].GetProperty("calories").GetInt32().Should().Be(2000);

        arr[1].GetProperty("id").GetInt64().Should().Be(firstId);
        arr[1].GetProperty("label").GetString().Should().Be("first");
        arr[1].GetProperty("calories").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task Delete_RemovesOwnedEntry()
    {
        var client = await _factory.RegisterAndLoginAsync("calc-del@test.com", "password1", "Calc Del");

        var add = await client.PostAsJsonAsync("/api/calc-history", Json("""{"calories":1234}"""));
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = JsonDocument.Parse(await add.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();

        var del = await client.DeleteAsync($"/api/calc-history/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResp = await client.GetAsync("/api/calc-history");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        arr.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_RequiresAuth_Returns401WhenAnonymous()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.GetAsync("/api/calc-history");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_OtherUsersEntry_IsNoOp_AndKeepsRow()
    {
        // OWNER ISOLATION: user A creates an entry; user B cannot delete it.
        var userA = await _factory.RegisterAndLoginAsync("calc-owner-a@test.com", "password1", "Owner A");
        var add = await userA.PostAsJsonAsync("/api/calc-history", Json("""{"calories":777}"""));
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var idA = JsonDocument.Parse(await add.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();

        var userB = await _factory.RegisterAndLoginAsync("calc-owner-b@test.com", "password1", "Owner B");
        var delByB = await userB.DeleteAsync($"/api/calc-history/{idA}");
        delByB.StatusCode.Should().Be(HttpStatusCode.OK); // no-op, still 200

        // user B sees an empty list (cannot read A's rows)
        var listB = await userB.GetAsync("/api/calc-history");
        var arrB = JsonDocument.Parse(await listB.Content.ReadAsStringAsync()).RootElement;
        arrB.GetArrayLength().Should().Be(0);

        // user A's row still exists
        var listA = await userA.GetAsync("/api/calc-history");
        var arrA = JsonDocument.Parse(await listA.Content.ReadAsStringAsync()).RootElement;
        arrA.GetArrayLength().Should().Be(1);
        arrA[0].GetProperty("id").GetInt64().Should().Be(idA);
        arrA[0].GetProperty("calories").GetInt32().Should().Be(777);
    }
}
