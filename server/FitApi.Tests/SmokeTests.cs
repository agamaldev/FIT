using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200_and_status_ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task Root_serves_index_html_containing_brand()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MAKE ME FIT");
    }
}
