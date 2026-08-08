namespace WebApp.Components.Pages.Settings.Shared;

/// <summary>
/// How list + editor are composed on settings catalog pages (Providers, Accounts, …).
/// Set on <see cref="SettingsEditorHost"/> via <c>Layout</c> or host class <c>settings-editor-layout-{mode}</c>.
/// </summary>
public enum SettingsEditorLayout
{
    /// <summary>List OR full-page editor (mode switch).</summary>
    FullPage,

    /// <summary>List stays visible; editor in <c>&lt;dialog&gt;</c>.</summary>
    Dialog,

    /// <summary>Master–detail columns on wide viewports; collapses to full-page on narrow when editing.</summary>
    SplitPane
}

public static class SettingsEditorLayoutExtensions
{
    public static string ToLayoutClass(this SettingsEditorLayout layout) =>
        layout switch
        {
            SettingsEditorLayout.Dialog => "settings-editor-layout-dialog",
            SettingsEditorLayout.SplitPane => "settings-editor-layout-split",
            _ => "settings-editor-layout-fullpage"
        };
}
