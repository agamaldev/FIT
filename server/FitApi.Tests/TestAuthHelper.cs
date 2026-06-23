using System.Net.Http.Json;

namespace FitApi.Tests;

public static class TestAuthHelper
{
    public static async Task<HttpClient> RegisterAndLoginAsync(
        this CustomWebApplicationFactory f,
        string email,
        string password,
        string name = "Test")
    {
        // WebApplicationFactory's client owns a cookie-capable handler, so
        // Set-Cookie from /login is replayed on subsequent requests automatically.
        var client = f.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { name, email, password });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        login.EnsureSuccessStatusCode();

        return client;
    }
}
