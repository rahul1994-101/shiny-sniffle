using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace WebApp.Utilities.Helpers;

internal static class AuthCookieHelpers
{
    internal static Task SignInAsync(
        HttpContext httpContext,
        Guid userId,
        string email,
        string fullName,
        bool isPersistent = true)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString("D")),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, fullName)
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
