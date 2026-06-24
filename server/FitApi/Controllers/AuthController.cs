using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FitApi.Dtos;
using FitApi.Models;
using FitApi.Services;

namespace FitApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IEmailSender _email;

    public AuthController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        IEmailSender email)
    {
        _users = users;
        _signIn = signIn;
        _email = email;
    }

    // The single UserDto mapping. Other tasks reference api/auth/me; they do NOT redefine this.
    private static UserDto ToUserDto(ApplicationUser u) =>
        new(u.Id, u.Email ?? "", u.DisplayName ?? "", u.PhotoUrl, u.EmailConfirmed);

    private static IDictionary<string, object?> Code(string code) =>
        new Dictionary<string, object?> { ["code"] = code };

    private async Task SendVerificationEmailAsync(ApplicationUser user)
    {
        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var encoded = Uri.EscapeDataString(token);
        var link =
            $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email" +
            $"?userId={Uri.EscapeDataString(user.Id)}&token={encoded}";
        var html =
            $"<p>To confirm your email address, click the link below:</p>" +
            $"<p><a href=\"{link}\">Confirm Email</a></p>";
        await _email.SendAsync(user.Email!, "Confirm your email", html);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var existing = await _users.FindByEmailAsync(req.Email);
        if (existing is not null)
            return Problem(statusCode: 409, title: "email-already-in-use",
                extensions: Code("email-already-in-use"));

        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.Name,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var dup = result.Errors.Any(e =>
                e.Code == nameof(IdentityErrorDescriber.DuplicateEmail) ||
                e.Code == nameof(IdentityErrorDescriber.DuplicateUserName));
            if (dup)
                return Problem(statusCode: 409, title: "email-already-in-use",
                    extensions: Code("email-already-in-use"));
            return Problem(statusCode: 400, title: "register-failed",
                extensions: Code("register-failed"));
        }

        // Best-effort: account creation must succeed even if email can't be sent
        // (e.g. local SQLite dev with no SMTP server). Matches the original behavior.
        try { await SendVerificationEmailAsync(user); }
        catch (Exception ex) { Console.Error.WriteLine($"[FIT] verification email failed: {ex.Message}"); }
        return Ok(new { });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null)
            return Problem(statusCode: 401, title: "invalid-credential",
                extensions: Code("invalid-credential"));

        var check = await _signIn.PasswordSignInAsync(
            user, req.Password, isPersistent: true, lockoutOnFailure: false);
        if (!check.Succeeded)
            return Problem(statusCode: 401, title: "invalid-credential",
                extensions: Code("invalid-credential"));

        return Ok(ToUserDto(user));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok(new { });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user is null)
            return Problem(statusCode: 401, title: "invalid-credential",
                extensions: Code("invalid-credential"));
        return Ok(ToUserDto(user));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is not null)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var link =
                $"{Request.Scheme}://{Request.Host}/reset-password.html" +
                $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
            var html =
                $"<p>To reset your password, click the link below:</p>" +
                $"<p><a href=\"{link}\">Reset Password</a></p>";
            await _email.SendAsync(user.Email!, "Reset your password", html);
        }
        // No account enumeration: always 200 regardless of existence.
        return Ok(new { });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null)
            return Problem(statusCode: 400, title: "invalid-token",
                extensions: Code("invalid-token"));

        var result = await _users.ResetPasswordAsync(user, req.Token, req.Password);
        if (!result.Succeeded)
            return Problem(statusCode: 400, title: "invalid-token",
                extensions: Code("invalid-token"));

        return Ok(new { });
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string userId, [FromQuery] string token)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null)
            return Redirect("/confirm-email.html?verified=0");

        var result = await _users.ConfirmEmailAsync(user, token);
        return Redirect(result.Succeeded
            ? "/confirm-email.html?verified=1"
            : "/confirm-email.html?verified=0");
    }

    [Authorize]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification()
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user is null)
            return Problem(statusCode: 401, title: "invalid-credential",
                extensions: Code("invalid-credential"));

        if (user.EmailConfirmed)
            return Ok(new { });

        await SendVerificationEmailAsync(user);
        return Ok(new { });
    }
}
