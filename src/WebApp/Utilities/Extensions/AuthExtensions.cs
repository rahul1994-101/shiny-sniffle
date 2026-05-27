using WebApp.Models;

namespace WebApp.Utilities.Extensions;

public static class AuthExtensions
{
    public static bool IsLocalReturnUrl(this string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        return returnUrl.StartsWith("/", StringComparison.Ordinal)
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.StartsWith("/\\", StringComparison.Ordinal);
    }

    public static string NormalizeReturnUrl(this string? returnUrl) =>
        returnUrl.IsLocalReturnUrl() ? returnUrl! : AuthConstants.DefaultReturnUrl;
}
