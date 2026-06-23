using FitApi.Dtos;
using FitApi.Models;
using FitApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitApi.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAvatarStorage _avatars;

    public ProfileController(UserManager<ApplicationUser> users, IAvatarStorage avatars)
    {
        _users = users;
        _avatars = avatars;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var user = (await _users.GetUserAsync(User))!;
        return Ok(ToDto(user));
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update([FromBody] UpdateProfileRequest req)
    {
        var user = (await _users.GetUserAsync(User))!;

        if (req.DisplayName is not null) user.DisplayName = req.DisplayName;
        if (req.Weight is not null) user.Weight = req.Weight;
        if (req.Height is not null) user.Height = req.Height;
        if (req.Goal is not null) user.Goal = req.Goal;
        if (req.Activity is not null) user.Activity = req.Activity;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return Problem(title: "update-failed", statusCode: 500);
        return Ok(ToDto(user));
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> Avatar([FromForm] IFormFile? file)
    {
        if (file is null
            || string.IsNullOrEmpty(file.ContentType)
            || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || file.Length > 2 * 1024 * 1024)
        {
            return Problem(
                statusCode: 400,
                title: "bad-avatar",
                extensions: new Dictionary<string, object?> { ["code"] = "bad-avatar" });
        }

        var user = (await _users.GetUserAsync(User))!;

        await using var stream = file.OpenReadStream();
        var url = await _avatars.SaveAsync(user.Id, stream, file.ContentType);

        user.PhotoUrl = url;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return Problem(title: "update-failed", statusCode: 500);

        return Ok(new { photoURL = url });
    }

    private static ProfileDto ToDto(ApplicationUser u) => new(
        u.DisplayName ?? "",
        u.Email ?? "",
        u.PhotoUrl,
        u.Weight,
        u.Height,
        u.Goal,
        u.Activity);
}
