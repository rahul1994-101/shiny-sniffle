using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

using WebApp.Features._Shared.Abstractions;
using WebApp.Features.User.Commands;
using WebApp.Utilities.Extensions;
using WebApp.Utilities.Helpers;

namespace WebApp.Endpoints;

[Route("api/auth")]
public sealed class AuthEndpoints(IFeatureSender sender, IAntiforgery _antiforgery) : Controller
{
    [HttpPost("login")]
    public async Task<IActionResult> SignIn([FromForm] SignInRequest signInRequest, [FromForm(Name = AuthConstants.ReturnUrlQuery)] string? returnUrl)
    {
        if (!await TryValidateAntiforgeryAsync())
        {
            return LocalRedirect(AuthExtensions.LoginUrl(returnUrl, "Invalid request. Please try again."));
        }

        var result = await sender.SendAsync(new SignInCommand(signInRequest));
        if (result.HasError || result.Payload is null)
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? "Invalid email or password.";
            return LocalRedirect(AuthExtensions.LoginUrl(returnUrl, message));
        }

        await AuthCookieHelpers.SignInAsync(HttpContext, result.Payload);

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
            await _antiforgery.ValidateRequestAsync(HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private Task SignOutAsync() =>
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    #endregion
}
