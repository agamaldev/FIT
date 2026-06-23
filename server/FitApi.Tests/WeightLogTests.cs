using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class WeightLogTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WeightLogTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static string Email() => $"weight_{Guid.NewGuid():N}@test.local";

    [Fact]
    public async Task Post_valid_entry_returns_201_with_dto()
    {
        var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234", "Weighty");

        var resp = await client.PostAsJsonAsync("/api/weight-log", new
        {
            weight = 82.5m,
            waist = 90m,
            chest = 100m,
            arms = 35m,
            date = "2026-06-20"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("id").GetInt64().Should().BeGreaterThan(0);
        dto.GetProperty("date").GetString().Should().Be("2026-06-20");
        dto.GetProperty("weight").GetDecimal().Should().Be(82.5m);
        dto.GetProperty("waist").GetDecimal().Should().Be(90m);
        dto.GetProperty("chest").GetDecimal().Should().Be(100m);
        dto.GetProperty("arms").GetDecimal().Should().Be(35m);
    }

    [Fact]
    public async Task Post_null_date_defaults_to_today()
    {
        var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

        var resp = await client.PostAsJsonAsync("/api/weight-log", new { weight = 70m });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var expected = DateTime.UtcNow.ToString("yyyy-MM-dd");
        dto.GetProperty("date").GetString().Should().Be(expected);
        dto.GetProperty("waist").ValueKind.Should().Be(JsonValueKind.Null);
        dto.GetProperty("chest").ValueKind.Should().Be(JsonValueKind.Null);
        dto.GetProperty("arms").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Post_weight_below_min_returns_400_bad_weight()
    {
        var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

        var resp = await client.PostAsJsonAsync("/api/weight-log", new { weight = 5m });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("bad-weight");
    }

    [Fact]
    public async Task Post_weight_above_max_returns_400_bad_weight()
    {
        var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

        var resp = await client.PostAsJsonAsync("/api/weight-log", new { weight = 600m });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("bad-weight");
    }

    [Fact]
    public async Task Get_returns_entries_ordered_by_date_desc()
    {
        var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

        await client.PostAsJsonAsync("/api/weight-log", new { weight = 80m, date = "2026-01-01" });
        await client.PostAsJsonAsync("/api/weight-log", new { weight = 81m, date = "2026-03-15" });
        await client.PostAsJsonAsync("/api/weight-log", new { weight = 82m, date = "2026-02-10" });

        var resp = await client.GetAsync("/api/weight-log");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var arr = await resp.Content.ReadFromJsonAsync<JsonElement>();
        arr.ValueKind.Should().Be(JsonValueKind.Array);
        var dates = arr.EnumerateArray().Select(e => e.GetProperty("date").GetString()).ToList();
        dates.Should().ContainInOrder("2026-03-15", "2026-02-10", "2026-01-01");
    }

    [Fact]
    public async Task Delete_removes_entry()
    {
        var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

        var post = await client.PostAsJsonAsync("/api/weight-log", new { weight = 75m, date = "2026-05-05" });
        var created = await post.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt64();

        var del = await client.DeleteAsync($"/api/weight-log/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await client.GetAsync("/api/weight-log");
        var arr = await after.Content.ReadFromJsonAsync<JsonElement>();
        arr.EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).Should().NotContain(id);
    }

    [Fact]
    public async Task Anonymous_get_returns_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/weight-log");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Owner_isolation_userB_cannot_read_or_delete_userA_entries()
    {
        var clientA = await _factory.RegisterAndLoginAsync(Email(), "pw1234", "UserA");
        var clientB = await _factory.RegisterAndLoginAsync(Email(), "pw1234", "UserB");

        var postA = await clientA.PostAsJsonAsync("/api/weight-log", new { weight = 88m, date = "2026-04-04" });
        var createdA = await postA.Content.ReadFromJsonAsync<JsonElement>();
        var idA = createdA.GetProperty("id").GetInt64();

        // B cannot see A's entry
        var bList = await clientB.GetAsync("/api/weight-log");
        var bArr = await bList.Content.ReadFromJsonAsync<JsonElement>();
        bArr.EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).Should().NotContain(idA);

        // B's delete of A's id is a no-op (still 200) and A still has the row
        var bDel = await clientB.DeleteAsync($"/api/weight-log/{idA}");
        bDel.StatusCode.Should().Be(HttpStatusCode.OK);

        var aList = await clientA.GetAsync("/api/weight-log");
        var aArr = await aList.Content.ReadFromJsonAsync<JsonElement>();
        aArr.EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).Should().Contain(idA);
    }
}
