using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace FitApi.Services;

public class LocalAvatarStorage : IAvatarStorage
{
    private readonly string _root;

    public LocalAvatarStorage(IConfiguration config, IWebHostEnvironment env)
    {
        _root = config["Storage:AvatarRoot"]
            ?? Path.Combine(env.ContentRootPath, "uploads", "avatars");
    }

    public async Task<string> SaveAsync(string userId, Stream content, string contentType)
    {
        var ext = contentType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            _ => ".img"
        };

        Directory.CreateDirectory(_root);

        var leafName = Path.GetFileName(userId + ext);
        var fullPath = Path.Combine(_root, leafName);

        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs);
        }

        return $"/uploads/avatars/{leafName}";
    }
}
