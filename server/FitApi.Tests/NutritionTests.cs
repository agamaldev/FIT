using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class NutritionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NutritionTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Save_Then_List_Returns_Stored_Plan_With_ComputedId_And_SavedAt()
    {
        var client = await _factory.RegisterAndLoginAsync("nut-save@test.local", "passw0rd", "Nut Save");

        // No explicit "id" -> server computes "{calId}-{varId}".
        var plan = JsonSerializer.Deserialize<JsonElement>(
            """{"calId":"1800","varId":"lowcarb","title":"خطة 1800"}""");

        var saveResp = await client.PostAsJsonAsync("/api/nutrition-plans", plan);
        saveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var saveDoc = JsonDocument.Parse(await saveResp.Content.ReadAsStringAsync());
        saveDoc.RootElement.GetProperty("saved").GetBoolean().Should().BeTrue();

        var listResp = await client.GetAsync("/api/nutrition-plans");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());

        listDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        listDoc.RootElement.GetArrayLength().Should().Be(1);

        var stored = listDoc.RootElement[0];
        stored.GetProperty("id").GetString().Should().Be("1800-lowcarb");
        stored.GetProperty("calId").GetString().Should().Be("1800");
        stored.GetProperty("varId").GetString().Should().Be("lowcarb");
        stored.GetProperty("title").GetString().Should().Be("خطة 1800");
        stored.TryGetProperty("savedAt", out var savedAt).Should().BeTrue();
        savedAt.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Save_Same_PlanId_Twice_Returns_SavedFalse_And_No_Duplicate()
    {
        var client = await _factory.RegisterAndLoginAsync("nut-dup@test.local", "passw0rd", "Nut Dup");

        var plan = JsonSerializer.Deserialize<JsonElement>(
            """{"id":"plan-A","title":"خطة A"}""");

        var first = await client.PostAsJsonAsync("/api/nutrition-plans", plan);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await first.Content.ReadAsStringAsync())
            .RootElement.GetProperty("saved").GetBoolean().Should().BeTrue();

        var second = await client.PostAsJsonAsync("/api/nutrition-plans", plan);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await second.Content.ReadAsStringAsync())
            .RootElement.GetProperty("saved").GetBoolean().Should().BeFalse();

        var listResp = await client.GetAsync("/api/nutrition-plans");
        var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        listDoc.RootElement.GetArrayLength().Should().Be(1);
        listDoc.RootElement[0].GetProperty("id").GetString().Should().Be("plan-A");
    }

    [Fact]
    public async Task Delete_Removes_The_Plan()
    {
        var client = await _factory.RegisterAndLoginAsync("nut-del@test.local", "passw0rd", "Nut Del");

        var plan = JsonSerializer.Deserialize<JsonElement>(
            """{"id":"plan-DEL","title":"خطة للحذف"}""");

        (await client.PostAsJsonAsync("/api/nutrition-plans", plan)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var delResp = await client.DeleteAsync("/api/nutrition-plans/plan-DEL");
        delResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResp = await client.GetAsync("/api/nutrition-plans");
        var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        listDoc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Owner_Isolation_UserB_Cannot_See_Or_Delete_UserA_Plan()
    {
        var clientA = await _factory.RegisterAndLoginAsync("nut-A@test.local", "passw0rd", "User A");
        var clientB = await _factory.RegisterAndLoginAsync("nut-B@test.local", "passw0rd", "User B");

        var plan = JsonSerializer.Deserialize<JsonElement>(
            """{"id":"shared-id","title":"خطة A فقط"}""");

        (await clientA.PostAsJsonAsync("/api/nutrition-plans", plan)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        // B cannot see A's plan.
        var listB = await clientB.GetAsync("/api/nutrition-plans");
        var listBDoc = JsonDocument.Parse(await listB.Content.ReadAsStringAsync());
        listBDoc.RootElement.GetArrayLength().Should().Be(0);

        // B can save its OWN plan reusing the same PlanId (per-user uniqueness only).
        var saveB = await clientB.PostAsJsonAsync("/api/nutrition-plans", plan);
        saveB.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await saveB.Content.ReadAsStringAsync())
            .RootElement.GetProperty("saved").GetBoolean().Should().BeTrue();

        // B deleting "shared-id" must NOT remove A's row.
        (await clientB.DeleteAsync("/api/nutrition-plans/shared-id")).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var listA = await clientA.GetAsync("/api/nutrition-plans");
        var listADoc = JsonDocument.Parse(await listA.Content.ReadAsStringAsync());
        listADoc.RootElement.GetArrayLength().Should().Be(1);
        listADoc.RootElement[0].GetProperty("id").GetString().Should().Be("shared-id");
    }

    [Fact]
    public async Task Anonymous_Is_Unauthorized()
    {
        var anon = _factory.CreateClient();
        (await anon.GetAsync("/api/nutrition-plans")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }
}
