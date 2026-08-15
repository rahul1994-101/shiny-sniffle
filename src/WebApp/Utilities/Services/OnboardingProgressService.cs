using System.Text.Json;
using Microsoft.JSInterop;
using WebApp.Components.Shared;

namespace WebApp.Utilities.Services;

public sealed class OnboardingProgressService(IJSRuntime _jsRuntime)
{
    public async Task<IReadOnlySet<string>> GetCompletedIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("webAppOnboarding.getCompletedJson", cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var ids = JsonSerializer.Deserialize<List<string>>(json);
            return ids is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : ids.ToHashSet(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
        catch (InvalidOperationException)
        {
            // Static prerender — JS not available until after first interactive render.
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    public async Task SetStepCompletedAsync(string stepId, bool completed, CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("webAppOnboarding.setStepCompleted", cancellationToken, stepId, completed);
        }
        catch (InvalidOperationException)
        {
            // Ignore during prerender; interactive actions run after hydration.
        }
    }

    public Task MarkCompleteAsync(string stepId, CancellationToken cancellationToken = default) =>
        SetStepCompletedAsync(stepId, completed: true, cancellationToken);

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("webAppOnboarding.reset", cancellationToken);
        }
        catch (InvalidOperationException)
        {
        }
    }

    public OnboardingProgressSummary Summarize(IReadOnlySet<string> completedIds)
    {
        var steps = OnboardingJourney.ProgressSteps;
        var completed = steps.Count(s => completedIds.Contains(s.Id));
        return new OnboardingProgressSummary(completed, steps.Count);
    }
}

public sealed record OnboardingProgressSummary(int Completed, int Total)
{
    public int Remaining => Math.Max(0, Total - Completed);

    public bool IsComplete => Total > 0 && Completed >= Total;
}
