using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitApi.Dtos;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class WorkoutsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkoutsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Completed_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workouts/completed");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Complete_Twice_IsIdempotent()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "workouts-idempotent@test.local", "pass123", "Idem");

        var first = await client.PostAsync("/api/workouts/complete", null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstStats = await first.Content.ReadFromJsonAsync<StreakStats>();
        firstStats!.Total.Should().Be(1);
        firstStats.Streak.Should().Be(1);

        var second = await client.PostAsync("/api/workouts/complete", null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondStats = await second.Content.ReadFromJsonAsync<StreakStats>();
        secondStats!.Total.Should().Be(1);
        secondStats.Streak.Should().Be(1);

        var completed = await client.GetFromJsonAsync<string[]>("/api/workouts/completed");
        completed!.Should().HaveCount(1);
        completed[0].Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task DeleteComplete_RemovesToday()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "workouts-delete@test.local", "pass123", "Del");

        await client.PostAsync("/api/workouts/complete", null);

        var deleteResponse = await client.DeleteAsync("/api/workouts/complete");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteStats = await deleteResponse.Content.ReadFromJsonAsync<StreakStats>();
        deleteStats!.Total.Should().Be(0);
        deleteStats.Streak.Should().Be(0);
        deleteStats.LongestStreak.Should().Be(0);

        var completed = await client.GetFromJsonAsync<string[]>("/api/workouts/completed");
        completed!.Should().BeEmpty();
    }

    [Fact]
    public async Task OwnerIsolation_UserBCannotSeeUserACompleted()
    {
        var userA = await _factory.RegisterAndLoginAsync(
            "workouts-owner-a@test.local", "pass123", "A");
        var userB = await _factory.RegisterAndLoginAsync(
            "workouts-owner-b@test.local", "pass123", "B");

        await userA.PostAsync("/api/workouts/complete", null);

        var bCompleted = await userB.GetFromJsonAsync<string[]>("/api/workouts/completed");
        bCompleted!.Should().BeEmpty();

        var aCompleted = await userA.GetFromJsonAsync<string[]>("/api/workouts/completed");
        aCompleted!.Should().HaveCount(1);
    }
}
