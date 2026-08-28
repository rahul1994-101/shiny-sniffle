namespace Application.Features.Shared;

/// <summary>Shared matching and ranking for mention-picker search (client picker + server queries).</summary>
internal static class EntityRefMentionSearch
{
    internal const int DefaultLimit = 20;

    internal const int MaxLimit = 50;

    internal static IReadOnlyList<string> ExtractRecentAliases(
        EntityRefs.Kind kind,
        IReadOnlyList<string>? recentHandles)
    {
        if (recentHandles is null || recentHandles.Count == 0)
        {
            return [];
        }

        var prefix = EntityRefs.PrefixWithSeparator(kind);
        var aliases = new List<string>();

        foreach (var handle in recentHandles)
        {
            if (!handle.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var alias = handle[prefix.Length..];
            if (alias.Length > 0)
            {
                aliases.Add(alias);
            }
        }

        return aliases;
    }

    internal static bool MatchesAliasQuery(
        string alias,
        string primary,
        string? secondary,
        string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return true;
        }

        return MatchesQuery(alias, query)
            || MatchesQuery(primary, query)
            || (!string.IsNullOrWhiteSpace(secondary) && MatchesQuery(secondary, query))
            || IsFuzzyMatch(primary, query)
            || IsFuzzyMatch(alias, query);
    }

    internal static int Rank(string alias, string primary, string? secondary, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return 0;
        }

        if (alias.StartsWith(query, StringComparison.OrdinalIgnoreCase)
            || primary.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(secondary) && secondary.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (alias.Contains(query, StringComparison.OrdinalIgnoreCase)
            || primary.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(secondary) && secondary.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            return 2;
        }

        return 3;
    }

    internal static int RecentRank(string alias, IReadOnlyList<string> recentAliases)
    {
        for (var i = 0; i < recentAliases.Count; i++)
        {
            if (string.Equals(recentAliases[i], alias, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool MatchesQuery(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    internal static IReadOnlyList<EntityRefMentionItemDto> MergeGlobalResults(
        IReadOnlyList<EntityRefMentionItemDto> items,
        string? query,
        IReadOnlyList<string>? recentHandles,
        int limit)
    {
        var trimmedQuery = query?.Trim() ?? string.Empty;
        var list = items.ToList();

        if (string.IsNullOrEmpty(trimmedQuery) && recentHandles is { Count: > 0 })
        {
            return list
                .OrderBy(GlobalRecentSortKey)
                .ThenBy(item => item.PrimaryLabel, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();
        }

        return list
            .OrderBy(item => Rank(item.Alias, item.PrimaryLabel, item.SecondaryLabel, trimmedQuery))
            .ThenBy(item => item.PrimaryLabel, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();

        int GlobalRecentSortKey(EntityRefMentionItemDto item)
        {
            var handle = EntityRefs.Format(item.Kind, item.Alias);
            for (var i = 0; i < recentHandles!.Count; i++)
            {
                if (string.Equals(recentHandles[i], handle, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return recentHandles.Count;
        }
    }

    private static bool IsFuzzyMatch(string value, string query)
    {
        if (query.Length < 3 || value.Length == 0)
        {
            return false;
        }

        var valueIndex = 0;
        foreach (var ch in query)
        {
            var found = false;
            while (valueIndex < value.Length)
            {
                if (char.ToLowerInvariant(value[valueIndex]) == char.ToLowerInvariant(ch))
                {
                    found = true;
                    valueIndex++;
                    break;
                }

                valueIndex++;
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }
}
