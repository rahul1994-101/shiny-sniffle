using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using WebApp;
using WebApp.AI.Agents.Chat;
using WebApp.AI.Agents.Intent;
using WebApp.AI.Foundry;
using WebApp.AI.Memory;
using WebApp.AI.Orchestration;
using WebApp.Components;
using WebApp.Data;
using WebApp.Models;
using WebApp.Utilities.Services;

namespace WebApp;

public static class DependencyInject
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.InjectEndpoints();
        builder.Services.InjectRazorComponents();
        builder.Services.InjectAuth();

        builder.Services.InjectData(builder.Configuration);
        builder.Services.InjectAi(builder.Configuration);
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

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseStatusCodePagesWithReExecute("/page-not-found", createScopeForStatusCodePages: true);

        app.UseAntiforgery();

        app.MapStaticAssets();
        app
            .MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapControllers();
    }


    public static void InjectEndpoints(this IServiceCollection services)
    {
        services.AddControllers();
    }

    public static void InjectRazorComponents(this IServiceCollection services)
    {
        services
            .AddRazorComponents()
            .AddInteractiveServerComponents();
    }

    public static void InjectAuth(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                // Where to redirect unauthenticated or forbidden requests (not the logout action URL).
                options.LoginPath = AuthConstants.LoginPagePath;
                options.AccessDeniedPath = AuthConstants.LoginPagePath;
                // After sign-out, AuthEndpoints sends users here; cookie middleware uses the same target.
                options.LogoutPath = AuthConstants.LoginPagePath;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
    }

    public static void InjectData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CurrentUser>();
        services.AddDbContext<AppDbContext>();
        services.AddScoped<Features>();
        services.AddScoped<Persistence>();
    }

    public static void InjectAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionName));
        services.AddSingleton<FoundryAgentFactory>();

        services.AddScoped<ThreadMemoryProvider>();
        services.AddScoped<IntentClassificationAgent>();
        services.AddScoped<GeneralChatAgent>();
        services.AddScoped<IntentRouter>();
        services.AddScoped<ChatOrchestrator>();
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
