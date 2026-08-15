using WebApp.Components.Pages.Settings;

namespace WebApp.Components.Pages.Help;

public static class HelpBreadcrumbTrails
{
    public static readonly IReadOnlyList<SettingsBreadcrumbItem> GettingStarted =
    [
        new("Help", "/help")
    ];

    public static IReadOnlyList<SettingsBreadcrumbItem> ForModule(HelpModule module) =>
    [
        new("Help", "/help")
    ];

    public static IReadOnlyList<SettingsBreadcrumbItem> ForTopic(HelpModule module, HelpTopic topic) =>
    [
        new("Help", "/help"),
        new(module.Title, module.Href)
    ];
}
