using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebApp.Models;

namespace WebApp.Auth;

public sealed class CookieAuthenticationService
{
    public static string Scheme => CookieAuthenticationDefaults.AuthenticationScheme;

    public Task SignInAsync(HttpContext httpContext, SignInResponse user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
        };

        var identity = new ClaimsIdentity(claims, Scheme, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
        };

        return httpContext.SignInAsync(Scheme, principal, properties);
    }

    public Task SignOutAsync(HttpContext httpContext) => httpContext.SignOutAsync(Scheme);
}
