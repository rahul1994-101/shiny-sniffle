namespace Application.Features.Shared;

/// <summary>
/// Typed handles for AI, tools, and working memory — <see cref="Kind.Contact"/> and <see cref="Kind.Mailbox"/> only.
/// Plain <c>alias</c> stays in the database; catalog rows (e.g. <c>dbo.EmailProvider.slug</c>) are not part of this scheme.
/// Use <see cref="Format"/> / <see cref="TryParse"/> at boundaries.
/// </summary>
public static class EntityRefs
{
    public const char Separator = ':';

    public enum Kind
    {
        Contact,
        Mailbox
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

        kind = default;
        return false;
    }
}
