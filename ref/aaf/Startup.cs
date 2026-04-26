using AAF.Components;
using AAF.Data;
using AAF.Models;
using AAF.Utilities;

using Dapper;

using Microsoft.AspNetCore.Authentication.Cookies;

namespace AAF;

public static class DependencyInject
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.InjectRazorComponents();

        builder.Services.AddScoped<AuthService>();

        // Authentication & Authorization
        builder.Services.InjectAuthentication();
        builder.Services.InjectAuthorization();

        builder.Services.InjectTypeHandlers();

        builder.Services.AddDbContext<AppDbContext>();

        builder.Services.AddScoped<Features>();
        builder.Services.AddSingleton<Mailer>();
        builder.Services.AddScoped<Persistence>();
    }

    public static void UseServices(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseFrameRenderingForExternalSite();

        app.UseStaticFiles();
        app.UseStatusCodePagesWithRedirects("/page-not-found");

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app
            .MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }


    public static void InjectRazorComponents(this IServiceCollection services)
    {
        services
            .AddRazorComponents()
            .AddInteractiveServerComponents();
    }

    public static void InjectAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(x =>
            {
                x.Cookie.Name = "auth_cookie";
                x.Cookie.MaxAge = TimeSpan.FromHours(12);

                x.LoginPath = "/login";
                x.LogoutPath = "/logout";
                x.AccessDeniedPath = "/access-denied";
            });
    }

    public static void InjectAuthorization(this IServiceCollection services)
    {
        services
            .AddAuthorization(x =>
            {
                x.AddPolicy("OnlyForAliens", x => x.RequireClaim("IsAlien", "true"));
                //x.AddPolicy("IsAdmin", policy => policy.RequireClaim("userType", "1"));
                //x.AddPolicy("IsCustomer", policy => policy.RequireClaim("userType", "2"));

                //x.AddPolicy("MustHaveIdClaim", policy => policy.RequireClaim("uid"));
                //x.AddPolicy("IdShouldBe3", policy => policy.RequireClaim("uid", "3"));
                //x.AddPolicy("Over18Only", policy => policy.Requirements.Add(new MinimumAgeRequirement(18)));
            })
            .AddCascadingAuthenticationState();
    }

    public static void InjectTypeHandlers(this IServiceCollection services)
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
    }

    public static void UseFrameRenderingForExternalSite(this WebApplication app)
    {
        var orgSettings = app.Configuration.GetSection("OrgSettings").Get<OrgSettings>();
        var allowedFrameAncestors = orgSettings?.SiteUrl;

        app.Use(async (context, next) =>
        {
            var key = "Content-Security-Policy";
            var value = $"frame-ancestors 'self' {allowedFrameAncestors}";
            context.Response.Headers.Add(key, value);

            await next();
        });
    }
}