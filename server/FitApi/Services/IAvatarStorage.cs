namespace FitApi.Services;

public interface IAvatarStorage
{
    Task<string> SaveAsync(string userId, Stream content, string contentType);
}
