using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FitApi.Models;
using Xunit;

namespace FitApi.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    // Pulls userId + token out of a captured confirm/reset email link query string.
    private static (string UserId, string Token) ExtractUserIdAndToken(string body)
    {
        var start = body.IndexOf("http", StringComparison.Ordinal);
        var rest = body[start..];
        var end = rest.IndexOfAny(new[] { '"', '\'', ' ', '<', '\n', '\r' });
        var url = end < 0 ? rest : rest[..end];
        var qs = HttpUtility.ParseQueryString(new Uri(url).Query);
        return (qs["userId"] ?? "", qs["token"] ?? "");
    }

    private static string ExtractResetToken(string body)
    {
        var start = body.IndexOf("http", StringComparison.Ordinal);
        var rest = body[start..];
        var end = rest.IndexOfAny(new[] { '"', '\'', ' ', '<', '\n', '\r' });
        var url = end < 0 ? rest : rest[..end];
        var qs = HttpUtility.ParseQueryString(new Uri(url).Query);
        return qs["token"] ?? "";
    }

    [Fact]
    public async Task Register_persists_user_with_displayName_and_sends_verification_email()
    {
        var client = _factory.CreateClient();
        var before = _factory.Email.Sent.Count;
        var email = $"reg_{Guid.NewGuid():N}@x.com";

        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { name = "Ahmed", email, password = "secret1" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        user.Should().NotBeNull();
        user!.DisplayName.Should().Be("Ahmed");

        _factory.Email.Sent.Count.Should().Be(before + 1);
        _factory.Email.Sent.Last().To.Should().Be(email);
    }

    [Fact]
    public async Task Register_duplicate_email_returns_409_with_code()
    {
        var client = _factory.CreateClient();
        var email = $"dup_{Guid.NewGuid():N}@x.com";

        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "A", email, password = "secret1" })).EnsureSuccessStatusCode();

        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { name = "A", email, password = "secret1" });

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("email-already-in-use");
    }

    [Fact]
    public async Task Login_good_returns_UserDto_and_cookie_then_me_works()
    {
        var client = _factory.CreateClient();
        var email = $"login_{Guid.NewGuid():N}@x.com";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "Lin", email, password = "secret1" })).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "secret1" });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        login.Headers.TryGetValues("Set-Cookie", out _).Should().BeTrue();
        var dto = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement;
        dto.GetProperty("email").GetString().Should().Be(email);
        dto.GetProperty("displayName").GetString().Should().Be("Lin");

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await me.Content.ReadAsStringAsync())
            .RootElement.GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task Login_wrong_password_returns_401_with_code()
    {
        var client = _factory.CreateClient();
        var email = $"wrong_{Guid.NewGuid():N}@x.com";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "W", email, password = "secret1" })).EnsureSuccessStatusCode();

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "WRONGPASS" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("invalid-credential");
    }

    [Fact]
    public async Task Me_after_logout_returns_401()
    {
        var email = $"logout_{Guid.NewGuid():N}@x.com";
        var client = await _factory.RegisterAndLoginAsync(email, "secret1", "Out");

        (await client.PostAsJsonAsync("/api/auth/logout", new { })).EnsureSuccessStatusCode();

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_unknown_email_returns_200_and_sends_nothing()
    {
        var client = _factory.CreateClient();
        var before = _factory.Email.Sent.Count;

        var resp = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = $"nobody_{Guid.NewGuid():N}@x.com" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Email.Sent.Count.Should().Be(before);
    }

    [Fact]
    public async Task ForgotThenReset_changes_password()
    {
        var client = _factory.CreateClient();
        var email = $"reset_{Guid.NewGuid():N}@x.com";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "R", email, password = "oldpass1" })).EnsureSuccessStatusCode();

        var beforeForgot = _factory.Email.Sent.Count;
        (await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email })).EnsureSuccessStatusCode();
        _factory.Email.Sent.Count.Should().Be(beforeForgot + 1);

        var token = ExtractResetToken(_factory.Email.Sent.Last().Body);

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { email, token, password = "newpass1" });
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldLogin = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "oldpass1" });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLogin = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "newpass1" });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfirmEmail_with_captured_token_redirects_verified_1_and_sets_flag()
    {
        var client = _factory.CreateClient();
        var noRedirect = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var email = $"confirm_{Guid.NewGuid():N}@x.com";

        var before = _factory.Email.Sent.Count;
        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "C", email, password = "secret1" })).EnsureSuccessStatusCode();
        _factory.Email.Sent.Count.Should().Be(before + 1);

        var (userId, token) = ExtractUserIdAndToken(_factory.Email.Sent.Last().Body);
        userId.Should().NotBeEmpty();
        token.Should().NotBeEmpty();

        var resp = await noRedirect.GetAsync(
            $"/api/auth/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}");

        resp.StatusCode.Should().Be(HttpStatusCode.Found);
        resp.Headers.Location!.ToString().Should().Be("/confirm-email.html?verified=1");

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        user!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ResendVerification_authed_sends_email()
    {
        var email = $"resend_{Guid.NewGuid():N}@x.com";
        var client = await _factory.RegisterAndLoginAsync(email, "secret1", "Re");

        var before = _factory.Email.Sent.Count;
        var resp = await client.PostAsJsonAsync("/api/auth/resend-verification", new { });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Email.Sent.Count.Should().Be(before + 1);
        _factory.Email.Sent.Last().To.Should().Be(email);
    }
}
