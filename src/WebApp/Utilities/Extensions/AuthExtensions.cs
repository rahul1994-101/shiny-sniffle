using WebApp.Models;

namespace WebApp.Utilities.Extensions;

public static class AuthExtensions
{
    public static string LoginUrl(string? returnUrl = null, string? error = null)
    {
        var safeReturn = returnUrl.NormalizeReturnUrl();
        if (string.IsNullOrWhiteSpace(error))
        {
            return $"{AuthConstants.LoginPagePath}?{AuthConstants.ReturnUrlQuery}={Uri.EscapeDataString(safeReturn)}";
        }

        return
            $"{AuthConstants.LoginPagePath}?{AuthConstants.ErrorQuery}={Uri.EscapeDataString(error)}" +
            $"&{AuthConstants.ReturnUrlQuery}={Uri.EscapeDataString(safeReturn)}";
    }

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
