using System.Text.RegularExpressions;

namespace Application.Features.Shared;

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
