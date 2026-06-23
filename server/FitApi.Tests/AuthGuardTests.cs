using System.Net;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class AuthGuardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthGuardTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_request_to_authorized_api_endpoint_returns_401_without_redirect()
    {
        // Do NOT auto-follow redirects, so we can assert there is no 302->login.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
        response.Headers.Contains("Location").Should().BeFalse();
    }

    [Fact]
    public async Task Health_endpoint_is_anonymous_and_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
