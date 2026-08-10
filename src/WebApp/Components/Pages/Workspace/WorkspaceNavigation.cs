using WebApp.Components.Pages.Settings;

namespace WebApp.Components.Pages.Workspace;

public static class WorkspaceBreadcrumbTrails
{
    public static readonly IReadOnlyList<SettingsBreadcrumbItem> Contacts =
    [
        new("Workspace", "/workspace")
    ];

    public static readonly IReadOnlyList<SettingsBreadcrumbItem> EmailAccounts =
    [
        new("Workspace", "/workspace")
    ];

    public static readonly IReadOnlyList<SettingsBreadcrumbItem> Tags =
    [
        new("Workspace", "/workspace")
    ];

    public static readonly IReadOnlyList<SettingsBreadcrumbItem> Buckets =
    [
        new("Workspace", "/workspace")
    ];
}
