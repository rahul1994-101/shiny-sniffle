using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Models;
using WebApp.Utilities.Extensions;

namespace WebApp.Endpoints;

[Route("api/auth")]
public sealed class AuthEndpoints(Features features, IAntiforgery antiforgery) : Controller
{
    [HttpPost("login")]
    public async Task<IActionResult> SignIn(
        [FromForm] SignInRequest signInRequest,
        [FromForm(Name = AuthConstants.ReturnUrlQuery)] string? returnUrl)
    {
        if (!await TryValidateAntiforgeryAsync())
        {
            return LocalRedirect(AuthExtensions.LoginUrl(returnUrl, "Invalid request. Please try again."));
        }

        var result = await features.SignInAsync(signInRequest);
        if (result.HasError || result.Payload is null)
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? "Invalid email or password.";
            return LocalRedirect(AuthExtensions.LoginUrl(returnUrl, message));
        }

        await SignInAsync(result.Payload);

        return LocalRedirect(returnUrl.NormalizeReturnUrl());
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!await TryValidateAntiforgeryAsync())
        {
            return LocalRedirect(AuthExtensions.LoginUrl(error: "Invalid request. Please try again."));
        }

        await SignOutAsync();
        return LocalRedirect(AuthConstants.LoginPagePath);
    }

    #region # Private Helpers

    private async Task<bool> TryValidateAntiforgeryAsync()
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

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
