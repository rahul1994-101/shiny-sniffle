using Microsoft.AspNetCore.Localization;

using System.Globalization;

using WebApp;
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

        builder.Services.AddDbContext<AppDbContext>();

        builder.Services.AddScoped<Features>();
        builder.Services.AddScoped<Persistence>();
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

        app.UseAntiforgery();

        app.MapStaticAssets();
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
