namespace WebApp.Components.Pages.Settings.Shared;

/// <summary>
/// How list + editor are composed on settings catalog pages (Providers, Accounts, …).
/// Set on <see cref="SettingsEditorHost"/> via <c>Layout</c> or host class <c>settings-editor-layout-{mode}</c>.
/// Sticky layout picker (full page / dialog / split) is preview UX — on by default with per-page <c>LayoutPreferenceKey</c>. Leave it unless the product owner asks to remove it; set <c>ShowLayoutPicker="false"</c> only for pages that must not offer layout choice.
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
