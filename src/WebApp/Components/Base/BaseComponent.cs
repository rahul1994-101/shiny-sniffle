using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using WebApp.Data;
using WebApp.Utilities.Helpers;

namespace WebApp.Components.Base;

/// <summary>
/// Base class for interactive Blazor components. Provides shared injections and layout helpers.
/// Layout shells (e.g. MainLayout) should inherit <see cref="LayoutComponentBase"/> instead.
/// </summary>
public abstract class BaseComponent : ComponentBase
{
    #region # Init

    [Inject]
    protected Features Features { get; set; } = null!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    protected AuthHelpers Auth { get; set; } = null!;

    [CascadingParameter(Name = "NotifyLayoutRefresh")]
    public EventCallback NotifyLayoutRefresh { get; set; }

    #endregion

    #region # Helpers

    protected Task RefreshLayoutAsync() =>
        NotifyLayoutRefresh.HasDelegate
            ? NotifyLayoutRefresh.InvokeAsync()
            : Task.CompletedTask;

    #endregion
}
