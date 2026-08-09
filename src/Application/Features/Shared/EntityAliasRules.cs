using System.Text;
using System.Text.RegularExpressions;

namespace Application.Features.Shared;

/// <summary>Shared slug rules for per-user <c>alias</c> columns (contacts, mailboxes).</summary>
public static class EntityAliasRules
{
    public const int MaxLength = 64;

    private static readonly Regex NonSlugChars = new(@"[^a-z0-9\-]+", RegexOptions.Compiled);

    private static readonly Regex CollapseHyphens = new(@"-{2,}", RegexOptions.Compiled);

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string StemFromPersonName(string firstName, string lastName)
    {
        var parts = new[] { firstName.Trim(), lastName.Trim() }
            .Where(x => x.Length > 0)
            .Select(SlugifySegment);

        var combined = string.Join("-", parts.Where(x => x.Length > 0));
        return combined.Length > 0 ? Truncate(combined) : "contact";
    }

    public static string StemFromEmailAddress(string emailAddress)
    {
        var trimmed = emailAddress.Trim();
        var at = trimmed.IndexOf('@');
        var local = at > 0 ? trimmed[..at] : trimmed;
        var slug = SlugifySegment(local);
        return slug.Length > 0 ? Truncate(slug) : "mailbox";
    }

    public static string WithNumericSuffix(string stem, int index, string emptyStemFallback)
    {
        if (index <= 1)
        {
            return Truncate(string.IsNullOrEmpty(stem) ? emptyStemFallback : stem);
        }

        var baseStem = string.IsNullOrEmpty(stem) ? emptyStemFallback : stem;
        var suffix = $"-{index}";
        var maxStem = MaxLength - suffix.Length;
        if (maxStem < 1)
        {
            maxStem = 1;
        }

        var trimmedStem = baseStem.Length <= maxStem ? baseStem : baseStem[..maxStem].TrimEnd('-');
        if (trimmedStem.Length == 0)
        {
            trimmedStem = emptyStemFallback;
        }

        return trimmedStem + suffix;
    }

    private static string SlugifySegment(string value)
    {
        var lower = value.ToLowerInvariant();
        var normalized = lower.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.')
            {
                builder.Append('-');
            }
        }

        return CollapseHyphens.Replace(NonSlugChars.Replace(builder.ToString(), "-"), "-").Trim('-');
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLength ? value : value[..MaxLength].TrimEnd('-');
}
