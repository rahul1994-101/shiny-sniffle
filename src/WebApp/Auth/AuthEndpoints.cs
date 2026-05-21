using Microsoft.AspNetCore.Antiforgery;

using WebApp.Data;
using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", LoginAsync);
        app.MapGet("/auth/logout", LogoutAsync);
        app.MapGet("/logout", () => Results.Redirect(AuthConstants.LogoutPath));
    }

    private static async Task<IResult> LoginAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        Features features,
        AuthService auth)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var returnUrl = form[AuthConstants.ReturnUrlQuery].ToString();

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return RedirectToLogin(returnUrl, "Invalid request. Please try again.");
        }

        var signInRequest = new SignInRequest
        {
            EmailId = form["EmailId"].ToString(),
            Password = form["Password"].ToString()
        };

        var result = await features.SignInAsync(signInRequest);
        if (result.HasError || result.Payload is null)
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? "Invalid email or password.";
            return RedirectToLogin(returnUrl, message);
        }

        await auth.SignInAsync(result.Payload);

        var destination = AuthService.NormalizeReturnUrl(returnUrl);
        return Results.LocalRedirect(destination);
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext, AuthService auth)
    {
        await auth.SignOutAsync();
        return Results.LocalRedirect(AuthConstants.LoginPath);
    }

    private static IResult RedirectToLogin(string? returnUrl, string errorMessage)
    {
        var safeReturn = AuthService.IsLocalReturnUrl(returnUrl) ? returnUrl! : AuthConstants.DefaultReturnUrl;
        var loginUrl =
            $"{AuthConstants.LoginPath}?{AuthConstants.ErrorQuery}={Uri.EscapeDataString(errorMessage)}" +
            $"&{AuthConstants.ReturnUrlQuery}={Uri.EscapeDataString(safeReturn)}";
        return Results.LocalRedirect(loginUrl);
    }
}
