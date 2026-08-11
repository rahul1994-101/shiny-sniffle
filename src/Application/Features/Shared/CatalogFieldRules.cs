using System.Text.RegularExpressions;

namespace Application.Features.Shared;

/// <summary>Shared optional presentation fields on <c>dbo</c> catalog rows (e.g. email providers).</summary>
public static partial class CatalogFieldRules
{
    public const int SlugMaxLength = 64;

    public const int NoteMaxLength = 256;

    public const int ColorMaxLength = 9;

    public static string NormalizeSlug(string? slug) =>
        string.IsNullOrWhiteSpace(slug) ? string.Empty : slug.Trim().ToLowerInvariant();

    public static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        return trimmed.Length > ColorMaxLength ? trimmed[..ColorMaxLength] : trimmed;
    }

    public static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length > NoteMaxLength ? trimmed[..NoteMaxLength] : trimmed;
    }

    public static bool IsValidSlugFormat(string slug) =>
        !string.IsNullOrEmpty(slug) && SlugRegex().IsMatch(slug);

    public static string StemFromDisplayName(string displayName) =>
        EntityAliasRules.StemFromLabel(displayName);

    public static string SlugWithNumericSuffix(string stem, int index, string emptyStemFallback) =>
        EntityAliasRules.WithNumericSuffix(stem, index, emptyStemFallback);

    public static string? ValidateSlug(string? slug, bool required = true)
    {
        var normalized = NormalizeSlug(slug);
        if (string.IsNullOrEmpty(normalized))
        {
            return required ? "Slug is required." : null;
        }

        if (normalized.Length > SlugMaxLength)
        {
            return $"Slug must be {SlugMaxLength} characters or fewer.";
        }

        if (!IsValidSlugFormat(normalized))
        {
            return "Slug must use lowercase letters, numbers, and hyphens only.";
        }

        return null;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();
}
