namespace WebApp.Components.Pages.Settings;

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

    public static readonly IReadOnlyList<SettingsBreadcrumbItem> EmailProviders =
    [
        new("Settings", "/settings")
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
