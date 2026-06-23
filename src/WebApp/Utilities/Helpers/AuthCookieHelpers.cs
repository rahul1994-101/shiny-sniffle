using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

using WebApp.Models;

namespace WebApp.Utilities.Helpers;

internal static class AuthCookieHelpers
{
    internal static Task SignInAsync(HttpContext httpContext, SignInResponse user, bool isPersistent = true)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
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

        return httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }
}
