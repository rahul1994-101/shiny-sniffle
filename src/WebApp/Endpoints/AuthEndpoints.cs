using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Models;
using WebApp.Utilities.Extensions;

namespace WebApp.Endpoints;

[Route("auth")]
public sealed class AuthEndpoints(Features features) : Controller
{
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] SignInRequest signInRequest,
        [FromForm(Name = AuthConstants.ReturnUrlQuery)] string? returnUrl)
    {
        var result = await features.SignInAsync(signInRequest);
        if (result.HasError || result.Payload is null)
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? "Invalid email or password.";
            var safeReturn = returnUrl.NormalizeReturnUrl();
            var loginUrl =
                $"{AuthConstants.LoginPath}?{AuthConstants.ErrorQuery}={Uri.EscapeDataString(message)}" +
                $"&{AuthConstants.ReturnUrlQuery}={Uri.EscapeDataString(safeReturn)}";
            return LocalRedirect(loginUrl);
        }

        await SignInAsync(result.Payload);

        return LocalRedirect(returnUrl.NormalizeReturnUrl());
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await SignOutAsync();
        return LocalRedirect(AuthConstants.LoginPath);
    }

    #region # Private Helpers

    private async Task SignInAsync(SignInResponse user, bool isPersistent = true)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString("D")),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }

    private Task SignOutAsync() =>
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    #endregion
}
