namespace WebApp.Models;

public static class AuthConstants
{
    public const string LoginPath = "/login";
    public const string LoginPostPath = "/auth/login";
    public const string LogoutPath = "/auth/logout";
    public const string DefaultReturnUrl = "/";

    public const string ReturnUrlQuery = "returnUrl";
    public const string ErrorQuery = "error";
}
