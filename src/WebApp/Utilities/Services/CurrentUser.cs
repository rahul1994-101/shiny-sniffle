using System.Security.Claims;

namespace WebApp.Utilities.Services;

/// <summary>
/// Reads the signed-in user from the current HTTP context (cookie claims). Blazor UI only — not sign-in/out.
/// <see cref="UpdateDisplayName"/> overlays the cookie name after profile saves (interactive Blazor cannot refresh cookies mid-circuit).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor _httpContextAccessor)
{
    private string? _fullNameOverride;

    public Guid Id
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string Email =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string FullName =>
        _fullNameOverride
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? string.Empty;

    public void UpdateDisplayName(string fullName) =>
        _fullNameOverride = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
}
