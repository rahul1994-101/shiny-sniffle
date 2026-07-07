namespace WebApp.Models;

public static class AuthConstants
{
    public const string LoginPagePath = "/auth/login";
    public const string LoginApiPath = "/api/auth/login";
    public const string LogoutApiPath = "/api/auth/logout";
    public const string DefaultReturnUrl = "/";

    public const string ReturnUrlQuery = "returnUrl";
    public const string ErrorQuery = "error";
}
