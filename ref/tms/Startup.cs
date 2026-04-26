using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;
using System.Globalization;
using WebUI.Components;
using WebUI.Data;
using WebUI.Utilities.Helpers;

namespace WebUI;

public static class DependencyInject
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.InjectRazorComponents();

        builder.Services.InjectMudServices();

        builder.Services.AddDbContext<TenantDbContext>();

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

        app.UseRequestLocalization();

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        app.UseHttpsRedirection();

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

    public static void InjectMudServices(this IServiceCollection services)
    {
        services.AddMudServices();
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