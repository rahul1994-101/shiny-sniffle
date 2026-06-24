namespace WebApp.Components.Pages.Settings;

/// <summary>Tracks unsaved edits across General / Email tabs for navigation guards.</summary>
public sealed class SettingsPageContext : IDisposable
{
    private bool _profileDirty;
    private bool _passwordDirty;
    private bool _emailDirty;

    public event Action? Changed;

    public bool IsDirty => _profileDirty || _passwordDirty || _emailDirty;

    public void SetProfileDirty(bool dirty) => Set(ref _profileDirty, dirty);

    public void SetPasswordDirty(bool dirty) => Set(ref _passwordDirty, dirty);

    public void SetEmailDirty(bool dirty) => Set(ref _emailDirty, dirty);

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
