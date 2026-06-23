using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace FitApi.Tests;

// Fake external handler bound to scheme "Google". On the callback request it
// returns a successful AuthenticateResult carrying a fixed external identity,
// exactly as the real Google handler would after a round-trip — but offline.
public class FakeGoogleHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestEmail = "google.user@example.com";
    public const string TestName = "Google User";
    public const string TestNameId = "google-subject-12345";

    public FakeGoogleHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestNameId),
            new Claim(ClaimTypes.Email, TestEmail),
            new Claim(ClaimTypes.Name, TestName),
            new Claim("urn:google:picture", "https://example.com/avatar.png"),
        };
        var identity = new ClaimsIdentity(claims, GoogleDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, GoogleDefaults.AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class AuthGoogleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthGoogleTests(CustomWebApplicationFactory factory) => _factory = factory;

    private WebApplicationFactory<Program> FactoryWithFakeGoogle() =>
        _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureTestServices(services =>
            {
                // Replace the real Google handler type in the existing scheme builder so
                // the scheme name remains registered once but points to the fake handler.
                // This runs after all IConfigureOptions (including the real AddGoogle) so
                // the scheme entry already exists and we can mutate its HandlerType in place.
                services.PostConfigure<AuthenticationOptions>(o =>
                {
                    var existing = o.Schemes
                        .FirstOrDefault(s => s.Name == GoogleDefaults.AuthenticationScheme);
                    if (existing is not null)
                        existing.HandlerType = typeof(FakeGoogleHandler);
                });
                // Register FakeGoogleHandler in DI so the scheme provider can resolve it.
                services.AddTransient<FakeGoogleHandler>();
            });
        });

    [Fact]
    public async Task GoogleCallback_CreatesUser_AndCookieAuthenticatesMe()
    {
        var factory = FactoryWithFakeGoogle();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var callback = await client.GetAsync("/api/auth/google/callback");

        callback.StatusCode.Should().Be(HttpStatusCode.Redirect);
        callback.Headers.Location!.OriginalString.Should().Be("/profile.html");

        // The callback set the Identity application cookie on the shared handler;
        // the same client now hits an [Authorize] endpoint and must be the new user.
        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await me.Content.ReadFromJsonAsync<MeProbe>();
        dto.Should().NotBeNull();
        dto!.Email.Should().Be(FakeGoogleHandler.TestEmail);
        dto.DisplayName.Should().Be(FakeGoogleHandler.TestName);
    }

    [Fact]
    public async Task GoogleChallenge_ReturnsChallenge_NotServerError()
    {
        var factory = FactoryWithFakeGoogle();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var resp = await client.GetAsync("/api/auth/google");

        // Fake handler has no challenge override, so the default AuthenticationHandler
        // challenge yields 401 (Unauthorized). We assert it is NOT a 5xx server error
        // and is the expected 401 — we do NOT assert on any external redirect target.
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record MeProbe(string Id, string Email, string DisplayName, string? PhotoUrl, bool EmailConfirmed);
}
