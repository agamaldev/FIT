using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace FitApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().WithImage("postgres:16").Build();

    // server/FitApi.Tests/bin/Debug/net10.0 -> up 5 -> repo root (holds index.html)
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
        builder.UseSetting("Frontend:WebRoot", RepoRoot);
        builder.UseEnvironment("Testing");
        // FIT:FACTORY-SERVICES-END
    }

    public async Task InitializeAsync() => await _db.StartAsync();
    public new async Task DisposeAsync() => await _db.DisposeAsync();
}
