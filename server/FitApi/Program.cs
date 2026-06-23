using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<FitApi.Data.AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddIdentity<FitApi.Models.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<FitApi.Data.AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddScoped<FitApi.Services.IEmailSender, FitApi.Services.SmtpEmailSender>();
builder.Services.AddAuthentication().AddGoogle(o =>
{
    var cfg = builder.Configuration;
    o.ClientId = cfg["Authentication:Google:ClientId"] ?? "test-client-id";
    o.ClientSecret = cfg["Authentication:Google:ClientSecret"] ?? "test-client-secret";
});
builder.Services.AddScoped<FitApi.Services.IAvatarStorage, FitApi.Services.LocalAvatarStorage>();
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
app.UseAuthentication();
app.UseAuthorization();
var avatarRoot = builder.Configuration["Storage:AvatarRoot"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "uploads", "avatars");
Directory.CreateDirectory(avatarRoot);
var uploadsRoot = Path.GetFullPath(Path.Combine(avatarRoot, ".."));
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});
// FIT:MIDDLEWARE-END

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }
