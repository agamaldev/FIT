using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class ProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProfileTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    // A tiny but VALID 1x1 PNG (decodable header + IEND), as raw bytes.
    private static byte[] TinyPng() => new byte[]
    {
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,
        0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,
        0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
        0x08,0x06,0x00,0x00,0x00,0x1F,0x15,0xC4,
        0x89,0x00,0x00,0x00,0x0A,0x49,0x44,0x41,
        0x54,0x78,0x9C,0x63,0x00,0x01,0x00,0x00,
        0x05,0x00,0x01,0x0D,0x0A,0x2D,0xB4,0x00,
        0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,
        0x42,0x60,0x82
    };

    [Fact]
    public async Task NewUser_GetProfile_HasEmail_AndNullMetrics()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-new@example.com", "secret123", "New User");

        var resp = await client.GetAsync("/api/profile");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("email").GetString().Should().Be("profile-new@example.com");
        root.GetProperty("displayName").GetString().Should().Be("New User");
        root.GetProperty("photoUrl").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("weight").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("height").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("goal").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("activity").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task PutProfile_UpdatesFields_AndGetReflects()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-put@example.com", "secret123", "Old Name");

        var putBody = new
        {
            displayName = "Updated Name",
            weight = 80.5m,
            height = 178.0m,
            goal = "lose",
            activity = "high"
        };
        var put = await client.PutAsJsonAsync("/api/profile", putBody);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var putDoc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        var putRoot = putDoc.RootElement;
        putRoot.GetProperty("displayName").GetString().Should().Be("Updated Name");
        putRoot.GetProperty("weight").GetDecimal().Should().Be(80.5m);
        putRoot.GetProperty("height").GetDecimal().Should().Be(178.0m);
        putRoot.GetProperty("goal").GetString().Should().Be("lose");
        putRoot.GetProperty("activity").GetString().Should().Be("high");

        var get = await client.GetAsync("/api/profile");
        var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var getRoot = getDoc.RootElement;
        getRoot.GetProperty("displayName").GetString().Should().Be("Updated Name");
        getRoot.GetProperty("weight").GetDecimal().Should().Be(80.5m);
        getRoot.GetProperty("height").GetDecimal().Should().Be(178.0m);
        getRoot.GetProperty("goal").GetString().Should().Be("lose");
        getRoot.GetProperty("activity").GetString().Should().Be("high");
        getRoot.GetProperty("email").GetString().Should().Be("profile-put@example.com");
    }

    [Fact]
    public async Task PostAvatar_ValidPng_Returns200_WithPhotoUrl_AndGetReflects()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-avatar@example.com", "secret123", "Avatar User");

        using var content = new MultipartFormDataContent();
        var bytes = TinyPng();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "avatar.png");

        var resp = await client.PostAsync("/api/profile/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var url = doc.RootElement.GetProperty("photoURL").GetString();
        url.Should().NotBeNullOrEmpty();
        url!.Should().StartWith("/uploads/avatars/");
        url.Should().EndWith(".png");

        var get = await client.GetAsync("/api/profile");
        var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("photoUrl").GetString().Should().Be(url);
    }

    [Fact]
    public async Task PostAvatar_TextFile_Returns400_BadAvatar()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-badtype@example.com", "secret123", "Bad Type");

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("not an image"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(file, "file", "note.txt");

        var resp = await client.PostAsync("/api/profile/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("bad-avatar");
    }

    [Fact]
    public async Task PostAvatar_TooLarge_Returns400_BadAvatar()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-toobig@example.com", "secret123", "Too Big");

        using var content = new MultipartFormDataContent();
        // 2 MB + 1 byte of image/png -> exceeds 2*1024*1024 limit.
        var bytes = new byte[2 * 1024 * 1024 + 1];
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "big.png");

        var resp = await client.PostAsync("/api/profile/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("bad-avatar");
    }

    [Fact]
    public async Task OwnerIsolation_UserB_Put_DoesNotChange_UserA_Profile()
    {
        var clientA = await _factory.RegisterAndLoginAsync(
            "isoA@example.com", "secret123", "User A");
        var clientB = await _factory.RegisterAndLoginAsync(
            "isoB@example.com", "secret123", "User B");

        // A sets a distinct profile.
        var aBody = new { displayName = "A Display", weight = 70.0m, height = 170.0m, goal = "gain", activity = "low" };
        (await clientA.PutAsJsonAsync("/api/profile", aBody)).StatusCode.Should().Be(HttpStatusCode.OK);

        // B updates B's own profile (the only thing B's cookie can touch).
        var bBody = new { displayName = "B Display", weight = 99.0m, height = 199.0m, goal = "lose", activity = "high" };
        (await clientB.PutAsJsonAsync("/api/profile", bBody)).StatusCode.Should().Be(HttpStatusCode.OK);

        // A's profile must be untouched by B's write.
        var getA = await clientA.GetAsync("/api/profile");
        var docA = JsonDocument.Parse(await getA.Content.ReadAsStringAsync());
        var rootA = docA.RootElement;
        rootA.GetProperty("email").GetString().Should().Be("isoA@example.com");
        rootA.GetProperty("displayName").GetString().Should().Be("A Display");
        rootA.GetProperty("weight").GetDecimal().Should().Be(70.0m);
        rootA.GetProperty("height").GetDecimal().Should().Be(170.0m);
        rootA.GetProperty("goal").GetString().Should().Be("gain");
        rootA.GetProperty("activity").GetString().Should().Be("low");
    }
}
