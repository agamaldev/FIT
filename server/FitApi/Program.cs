using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// FIT:SERVICES-END

var app = builder.Build();

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
