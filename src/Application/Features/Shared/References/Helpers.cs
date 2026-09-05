using System.Text.RegularExpressions;

namespace Application.Features.Shared;

#region # EntityRefs

/// <summary>
/// Typed handles for AI, tools, and working memory.
/// Plain <c>alias</c> stays in workspace tables; catalog rows (e.g. <c>dbo.EmailProvider</c>) use <c>id</c> — not part of this scheme.
/// Use <see cref="Format"/> / <see cref="TryParse"/> at boundaries.
/// </summary>
public static class EntityRefs
{
    public const char Separator = ':';

    public enum Kind
    {
        Contact,
        Mailbox,
        Tag,
        Bucket
    }

    public static string Format(Kind kind, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new ArgumentException("Alias is required.", nameof(alias));
        }

        return $"{Prefix(kind)}{Separator}{alias.Trim()}";
    }

    public static string PrefixWithSeparator(Kind kind) => $"{Prefix(kind)}{Separator}";

    public static bool TryParse(string? value, out Kind kind, out string alias)
    {
        kind = default;
        alias = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var separator = trimmed.IndexOf(Separator);
        if (separator <= 0 || separator >= trimmed.Length - 1)
        {
            return false;
        }

        var prefix = trimmed[..separator];
        if (!TryKindFromPrefix(prefix, out kind))
        {
            return false;
        }

        alias = trimmed[(separator + 1)..].Trim();
        return alias.Length > 0;
    }

    internal static string Prefix(Kind kind) => kind switch
    {
        Kind.Contact => "contact",
        Kind.Mailbox => "mailbox",
        Kind.Tag => "tag",
        Kind.Bucket => "bucket",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static bool TryKindFromPrefix(ReadOnlySpan<char> prefix, out Kind kind)
    {
        if (prefix.Equals("contact", StringComparison.OrdinalIgnoreCase))
        {
            kind = Kind.Contact;
            return true;
        }

        if (prefix.Equals("mailbox", StringComparison.OrdinalIgnoreCase))
        {
            kind = Kind.Mailbox;
            return true;
        }

        if (prefix.Equals("tag", StringComparison.OrdinalIgnoreCase))
        {
            kind = Kind.Tag;
            return true;
        }

        if (prefix.Equals("bucket", StringComparison.OrdinalIgnoreCase))
        {
            kind = Kind.Bucket;
            return true;
        }

        kind = default;
        return false;
    }
}

#endregion

#region # Mentions

public readonly record struct EntityRefMentionSegment(string Text, bool IsMention, string? Handle);

/// <summary>Parse <c>@kind:alias</c> tokens from user-authored text.</summary>
public static class EntityRefMentions
{
    private static readonly Regex TokenPattern = new(
        @"@(?<handle>(?:contact|mailbox|tag|bucket):[a-z0-9][a-z0-9\-]*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return TokenPattern
            .Matches(text)
            .Select(match => match.Groups["handle"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> ExtractMailboxHandles(string? text) =>
        ExtractFromText(text)
            .Where(handle => EntityRefs.TryParse(handle, out var kind, out _) && kind == EntityRefs.Kind.Mailbox)
            .ToList();

    public static IReadOnlyList<EntityRefMentionSegment> ParseSegments(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var segments = new List<EntityRefMentionSegment>();
        var lastIndex = 0;

        foreach (Match match in TokenPattern.Matches(text))
        {
            if (match.Index > lastIndex)
            {
                segments.Add(new EntityRefMentionSegment(text[lastIndex..match.Index], false, null));
            }

            segments.Add(new EntityRefMentionSegment(match.Value, true, match.Groups["handle"].Value));
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            segments.Add(new EntityRefMentionSegment(text[lastIndex..], false, null));
        }

        return segments;
    }
}

#endregion

#region # Search

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

#endregion

internal static class EntityRefResolverCopy
{
    internal static string KindLabel(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "contact",
        EntityRefs.Kind.Mailbox => "mailbox",
        EntityRefs.Kind.Tag => "tag",
        EntityRefs.Kind.Bucket => "bucket",
        _ => "item"
    };
}
