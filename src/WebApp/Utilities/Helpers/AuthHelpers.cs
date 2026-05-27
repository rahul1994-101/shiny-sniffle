using System.Security.Claims;

namespace WebApp.Utilities.Helpers;

/// <summary>
/// Reads the signed-in user from the current HTTP context (cookie claims). Blazor UI only — not sign-in/out.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid Id
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string FullName =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}
