namespace Application.Features.Shared;

/// <summary>
/// Typed handles for AI, tools, and working memory.
/// Plain <c>alias</c> stays in workspace tables; <c>dbo.EmailProvider.slug</c> is catalog-only (not part of this scheme).
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
