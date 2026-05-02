using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;

using System.Globalization;

using WebApp;
using WebApp._PocStuff;
using WebApp.Components;
using WebApp.Data;
using WebApp.Utilities.Helpers;

namespace WebApp;

public static class DependencyInject
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.InjectRazorComponents();

        // To be Migrated..
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        builder.Services.AddSingleton<CookieAuthenticationService>();

        builder.Services.InjectAuthentication();
        builder.Services.InjectAuthorization();

        builder.Services.AddDbContext<AppDbContext>();

        builder.Services.AddScoped<Features>();
        builder.Services.AddScoped<Persistence>();

        // To be Migrated..
        builder.Services.Configure<AgenticApiOptions>(
            builder.Configuration.GetSection(AgenticApiOptions.SectionName)
        );
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<AgenticApiClient>();
        builder.Services.AddScoped<Repository>();
        builder.Services.AddScoped<Service>();
    }

    public static void UseServices(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRequestLocalization();

        app.UseStatusCodePagesWithReExecute("/page-not-found", createScopeForStatusCodePages: true);

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapLangChainApi(); // To be Migrated
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
                x.SlidingExpiration = true;

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

    public static void UseRequestLocalization(this WebApplication app)
    {
        var cultureInfo = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(cultureInfo),
            SupportedCultures = [cultureInfo],
            SupportedUICultures = [cultureInfo]
        });
    }
}