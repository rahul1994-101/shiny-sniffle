using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using WebApp;
using WebApp.Components;
using WebApp.Data;
using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp;

public static class DependencyInject
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.InjectRazorComponents();

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
        builder.Services.AddScoped<ProtectedLocalStorage>();
        builder.Services.AddScoped<MockSessionPersistence>();
    }

    public static void UseServices(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseRequestLocalization();

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

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