using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Infrastructure.Persistence.Shared;

namespace Application.Features.Shared;

/// <summary>Queryable filters for <see cref="BaseEntity.IsActive"/> / <see cref="BaseEntity.IsDeleted"/>.</summary>
internal static class EntityLifecycleQueries
{
    /// <summary><c>IsActive</c> only — narrow an already-not-deleted set to runtime-usable rows.</summary>
    internal static IQueryable<T> WhereIsActive<T>(this IQueryable<T> query) where T : BaseEntity =>
        query.Where(x => x.IsActive);

    /// <summary><c>!IsDeleted</c> only — row still owned by the user (includes paused/inactive rows).</summary>
    internal static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> query) where T : BaseEntity =>
        query.Where(x => !x.IsDeleted);

    /// <summary>Runtime reads — active and not soft-deleted.</summary>
    internal static IQueryable<T> WhereActiveAndNotDeleted<T>(this IQueryable<T> query) where T : BaseEntity =>
        query.WhereIsActive().WhereNotDeleted();

    /// <summary>Paused rows — inactive but not soft-deleted.</summary>
    internal static IQueryable<T> WhereInactiveAndNotDeleted<T>(this IQueryable<T> query) where T : BaseEntity =>
        query.Where(x => !x.IsActive && !x.IsDeleted);
}

/// <summary>Shared slug rules for per-user <c>alias</c> columns (contacts, mailboxes).</summary>
public static class EntityAliasRules
{
    public const int MaxLength = 64;

    private static readonly Regex NonSlugChars = new(@"[^a-z0-9\-]+", RegexOptions.Compiled);

    private static readonly Regex CollapseHyphens = new(@"-{2,}", RegexOptions.Compiled);

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Slugify optional user-provided alias; null when blank or slugifies to empty.</summary>
    public static string? SlugifyOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var slug = SlugifySegment(value.Trim());
        slug = CollapseHyphens.Replace(NonSlugChars.Replace(slug, "-"), "-").Trim('-');
        return slug.Length > 0 ? Truncate(slug) : null;
    }

    /// <summary>Slug stem from a single display label (catalog name, etc.).</summary>
    public static string StemFromLabel(string value, string emptyFallback = "item")
    {
        var slug = SlugifySegment(value.Trim());
        return slug.Length > 0 ? Truncate(slug) : emptyFallback;
    }

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

    /// <summary>UI placeholder preview — empty when nothing slugifiable (no kind fallback).</summary>
    public static string PreviewPersonNameOrEmpty(string firstName, string lastName)
    {
        var parts = new[] { firstName.Trim(), lastName.Trim() }
            .Where(x => x.Length > 0)
            .Select(SlugifySegment);

        var combined = string.Join("-", parts.Where(x => x.Length > 0));
        return combined.Length > 0 ? Truncate(combined) : string.Empty;
    }

    /// <summary>UI placeholder preview — empty when no email local part (no kind fallback).</summary>
    public static string PreviewEmailLocalOrEmpty(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return string.Empty;
        }

        var trimmed = emailAddress.Trim();
        var at = trimmed.IndexOf('@');
        var local = at > 0 ? trimmed[..at] : trimmed;
        if (string.IsNullOrWhiteSpace(local))
        {
            return string.Empty;
        }

        var slug = SlugifySegment(local);
        return slug.Length > 0 ? Truncate(slug) : string.Empty;
    }

    /// <summary>UI placeholder preview — empty when label slugifies to nothing.</summary>
    public static string PreviewLabelOrEmpty(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var slug = SlugifySegment(value.Trim());
        return slug.Length > 0 ? Truncate(slug) : string.Empty;
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

/// <summary>Preview handles in workspace forms before save (matches server slugify rules).</summary>
public static class EntityAliasPreview
{
    public static string FromNameOrAlias(string? alias, string displayName, string emptyFallback)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            return EntityAliasRules.StemFromLabel(alias.Trim(), emptyFallback);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return string.Empty;
        }

        return EntityAliasRules.StemFromLabel(displayName.Trim(), emptyFallback);
    }

    /// <summary>Alias input placeholder stem — real slug preview only; never kind fallback tokens.</summary>
    public static string PlaceholderStem(
        EntityRefs.Kind kind,
        string? alias,
        string? primarySource,
        string? secondarySource = null)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            return EntityAliasRules.SlugifyOptional(alias) ?? string.Empty;
        }

        return kind switch
        {
            EntityRefs.Kind.Contact => EntityAliasRules.PreviewPersonNameOrEmpty(primarySource ?? string.Empty, secondarySource ?? string.Empty),
            EntityRefs.Kind.Mailbox => EntityAliasRules.PreviewEmailLocalOrEmpty(primarySource ?? string.Empty),
            EntityRefs.Kind.Tag or EntityRefs.Kind.Bucket => EntityAliasRules.PreviewLabelOrEmpty(primarySource ?? string.Empty),
            _ => string.Empty
        };
    }
}

/// <summary>Shared optional presentation fields on workspace catalog-style rows (tags, buckets).</summary>
public static class CatalogFieldRules
{
    public const int NoteMaxLength = 256;

    public const int ContextMaxLength = 2000;

    public const int ColorMaxLength = 9;

    private static readonly Regex HexColorRegex = new(@"^#[0-9a-fA-F]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Returns a validation error when color is non-blank but not a valid <c>#RRGGBB</c> hex value.</summary>
    public static string? ValidateColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        return TryNormalizeHexColor(color) is null
            ? "Color must be a valid hex value (e.g. #6366f1)."
            : null;
    }

    public static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        return TryNormalizeHexColor(color);
    }

    private static string? TryNormalizeHexColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var candidate = color.Trim();
        if (!candidate.StartsWith('#'))
        {
            candidate = $"#{candidate}";
        }

        if (candidate.Length != 7 || !HexColorRegex.IsMatch(candidate))
        {
            return null;
        }

        return candidate.ToLowerInvariant();
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

    public static string? NormalizeContext(string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return null;
        }

        var trimmed = context.Trim();
        return trimmed.Length > ContextMaxLength ? trimmed[..ContextMaxLength] : trimmed;
    }
}

public static class ReferableKindMapping
{
    public static ReferableKind ToPersistence(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => ReferableKind.Contact,
        EntityRefs.Kind.Mailbox => ReferableKind.Mailbox,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}

internal static class WorkspaceErAliasResolver
{
    /// <summary>
    /// Shared alias rules for workspace ER rows: slugify when provided; on edit with blank input keep
    /// <paramref name="existingAlias"/>; on create auto-generate from source fields with numeric suffix.
    /// </summary>
    internal static async Task<string> ResolveAsync(
        Func<string, Guid?, CancellationToken, Task<bool>> isTakenAsync,
        EntityRefs.Kind kind,
        string? requestedAlias,
        Guid? entityId,
        string? existingAlias,
        string primarySource,
        string? secondarySource,
        CancellationToken cancellationToken)
    {
        var normalized = EntityAliasRules.SlugifyOptional(requestedAlias);
        if (normalized is not null)
        {
            return normalized;
        }

        if (entityId is not null && !string.IsNullOrWhiteSpace(existingAlias))
        {
            return existingAlias.Trim();
        }

        var (stem, fallback) = BuildStem(kind, primarySource, secondarySource);

        for (var index = 1; index < 10_000; index++)
        {
            var candidate = EntityAliasRules.WithNumericSuffix(stem, index, fallback);
            if (!await isTakenAsync(candidate, entityId, cancellationToken))
            {
                return candidate;
            }
        }

        return EntityAliasRules.WithNumericSuffix(stem, Random.Shared.Next(1000, 9999), fallback);
    }

    private static (string Stem, string Fallback) BuildStem(
        EntityRefs.Kind kind,
        string primarySource,
        string? secondarySource) =>
        kind switch
        {
            EntityRefs.Kind.Contact => (
                EntityAliasRules.StemFromPersonName(primarySource, secondarySource ?? string.Empty),
                "contact"),
            EntityRefs.Kind.Mailbox => (
                EntityAliasRules.StemFromEmailAddress(primarySource),
                "mailbox"),
            EntityRefs.Kind.Tag => (
                EntityAliasRules.StemFromLabel(primarySource, "tag"),
                "tag"),
            EntityRefs.Kind.Bucket => (
                EntityAliasRules.StemFromLabel(primarySource, "bucket"),
                "bucket"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
}

