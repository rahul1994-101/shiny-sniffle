namespace WebApp.Components.Pages.Settings;

/// <summary>Shared confirm copy for unsaved editor and settings forms.</summary>
public static class SettingsDirtyGuard
{
    public const string LeaveMessage = "You have unsaved changes. Leave without saving?";

    public const string DiscardMessage = "You have unsaved changes. Discard them?";
}

/// <summary>Tracks unsaved edits for navigation guards on settings and workspace editor pages.</summary>
public sealed class SettingsPageContext : IDisposable
{
    private bool _profileDirty;
    private bool _passwordDirty;
    private bool _editorDirty;

    public event Action? Changed;

    public bool IsDirty => _profileDirty || _passwordDirty || _editorDirty;

    public void SetProfileDirty(bool dirty) => Set(ref _profileDirty, dirty);

    public void SetPasswordDirty(bool dirty) => Set(ref _passwordDirty, dirty);

    public void SetEditorDirty(bool dirty) => Set(ref _editorDirty, dirty);

    public void Dispose() => Changed = null;

    private void Set(ref bool field, bool value)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Changed?.Invoke();
    }
}

public sealed record SettingsSectionStatus(string Message, bool IsError, bool Fading = false);

/// <summary>One breadcrumb segment (ancestors only; current page is the H1).</summary>
public sealed record SettingsBreadcrumbItem(string Label, string? Href = null);

/// <summary>Shared breadcrumb trails for settings sub-pages (hub <c>/settings</c> uses none).</summary>
public static class SettingsBreadcrumbTrails
{
    public static readonly IReadOnlyList<SettingsBreadcrumbItem> General =
    [
        new("Settings", "/settings")
    ];

    public static readonly IReadOnlyList<SettingsBreadcrumbItem> Appearance =
    [
        new("Settings", "/settings")
    ];

    public static readonly IReadOnlyList<SettingsBreadcrumbItem> Email =
    [
        new("Settings", "/settings"),
        new("Email", "/settings/email/providers")
    ];
}

/// <summary>Inline save/error status with optional auto-fade for settings cards.</summary>
public sealed class SettingsSectionStatusHandle : IDisposable
{
    private readonly Func<Task> _refreshAsync;
    private CancellationTokenSource? _clearCts;

    public SettingsSectionStatusHandle(Func<Task> refreshAsync)
    {
        _refreshAsync = refreshAsync;
    }

    public SettingsSectionStatus? Current { get; private set; }

    public void Set(string message, bool isError, bool autoClear = false)
    {
        ClearPending();
        Current = new SettingsSectionStatus(message, isError);

        if (!isError && autoClear)
        {
            _clearCts = new CancellationTokenSource();
            _ = FadeAndClearAsync(_clearCts);
        }
    }

    public void Clear()
    {
        ClearPending();
        Current = null;
    }

    public void Dispose()
    {
        ClearPending();
    }

    private void ClearPending()
    {
        if (_clearCts is null)
        {
            return;
        }

        _clearCts.Cancel();
        _clearCts.Dispose();
        _clearCts = null;
    }

    private async Task FadeAndClearAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3.5), cts.Token);

            if (Current is not null && !Current.IsError)
            {
                Current = Current with { Fading = true };
                await _refreshAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(0.4), cts.Token);
            Current = null;
            await _refreshAsync();
        }
        catch (OperationCanceledException)
        {
            // replaced by a newer status
        }
    }
}
