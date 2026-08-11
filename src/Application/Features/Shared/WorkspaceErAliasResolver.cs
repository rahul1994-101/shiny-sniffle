namespace Application.Features.Shared;

internal static class WorkspaceErAliasResolver
{
    internal static async Task<string> ResolveAsync(
        Func<string, Guid?, CancellationToken, Task<bool>> isTakenAsync,
        string displayName,
        string? requestedAlias,
        Guid? excludeId,
        string emptyStemFallback,
        CancellationToken cancellationToken)
    {
        var normalized = EntityAliasRules.NormalizeOptional(requestedAlias);
        if (normalized is not null)
        {
            return normalized;
        }

        var stem = EntityAliasRules.StemFromLabel(displayName, emptyStemFallback);

        for (var index = 1; index < 10_000; index++)
        {
            var candidate = EntityAliasRules.WithNumericSuffix(stem, index, emptyStemFallback);
            if (!await isTakenAsync(candidate, excludeId, cancellationToken))
            {
                return candidate;
            }
        }

        return EntityAliasRules.WithNumericSuffix(stem, Random.Shared.Next(1000, 9999), emptyStemFallback);
    }
}
