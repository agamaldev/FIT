using System.Security.Claims;
using FitApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitApi.Controllers;

[ApiController]
[Route("api/auth/google")]
[AllowAnonymous]
public class AuthGoogleController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthGoogleController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // GET api/auth/google  -> challenge the external "Google" scheme; on success
    // Google redirects the browser back to our callback action.
    [HttpGet("")]
    public IActionResult Challenge([FromQuery] string? returnUrl = null)
    {
        var callbackUrl = Url.Action(nameof(Callback), "AuthGoogle", null, Request.Scheme);
        var props = _signInManager.ConfigureExternalAuthenticationProperties(
            GoogleDefaults.AuthenticationScheme, callbackUrl);
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            props.Items["returnUrl"] = returnUrl;
        }
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    // GET api/auth/google/callback -> read the external identity, find-or-create
    // the local user, link the external login, issue the app cookie, redirect.
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            // Fall back to authenticating the "Google" scheme directly (covers
            // the deterministic fake-scheme test where no auth session cookie
            // was set, and any case where GetExternalLoginInfoAsync returns null).
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal is null)
            {
                return Redirect("/profile.html");
            }

            var providerKey =
                result.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                result.Principal.FindFirstValue("sub") ??
                result.Principal.FindFirstValue(ClaimTypes.Email) ??
                Guid.NewGuid().ToString();

            info = new ExternalLoginInfo(
                result.Principal,
                GoogleDefaults.AuthenticationScheme,
                providerKey,
                GoogleDefaults.AuthenticationScheme);
        }

        var email =
            info.Principal.FindFirstValue(ClaimTypes.Email) ??
            info.Principal.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(email))
        {
            return Redirect("/profile.html");
        }

        // Already linked? sign in directly.
        var signIn = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        if (signIn.Succeeded)
        {
            return Redirect("/profile.html");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName =
                    info.Principal.FindFirstValue(ClaimTypes.Name) ??
                    info.Principal.FindFirstValue("name"),
                PhotoUrl =
                    info.Principal.FindFirstValue("urn:google:picture") ??
                    info.Principal.FindFirstValue("picture"),
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            var created = await _userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                return Redirect("/profile.html");
            }
        }

        // Link the external login if not already linked, then issue the app cookie.
        var logins = await _userManager.GetLoginsAsync(user);
        var alreadyLinked = logins.Any(l =>
            l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey);
        if (!alreadyLinked)
        {
            await _userManager.AddLoginAsync(user, info);
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return Redirect("/profile.html");
    }
}
