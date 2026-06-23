using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<FitApi.Data.AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
// FIT:SERVICES-END

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitApi.Data.AppDbContext>();
    db.Database.Migrate();
}
// FIT:STARTUP-END

var frontendRoot = builder.Configuration["Frontend:WebRoot"];
if (!string.IsNullOrWhiteSpace(frontendRoot))
{
    var provider = new PhysicalFileProvider(Path.GetFullPath(frontendRoot));
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}
// FIT:MIDDLEWARE-END

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }
