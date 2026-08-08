using WebApp.Components.Pages.Settings;

namespace WebApp.Components.Pages.Workspace;

public static class WorkspaceBreadcrumbTrails
{
    public static readonly IReadOnlyList<SettingsBreadcrumbItem> Contacts =
    [
        new("Workspace", "/workspace")
    ];
}
