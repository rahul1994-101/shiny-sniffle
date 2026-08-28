using Application.Features.Shared;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;

namespace WebApp.Components.Shared;

internal enum MentionPickerStep
{
    None,
    Kind,
    Alias
}

internal sealed class MentionContext
{
    public MentionPickerStep Step { get; init; }

    public int MentionStart { get; init; }

    public int Caret { get; init; }

    public string Query { get; init; } = string.Empty;

    public EntityRefs.Kind Kind { get; init; }
}

internal enum MentionOptionKind
{
    Kind,
    Alias
}

internal sealed class EntityRefMentionOption
{
    public MentionOptionKind OptionKind { get; init; }

    public EntityRefs.Kind? EntityKind { get; init; }

    public string PrimaryLabel { get; init; } = string.Empty;

    public string? SecondaryLabel { get; init; }

    public string InsertText { get; init; } = string.Empty;

    public bool ClosesPicker { get; init; }

    public string? AvatarText { get; init; }

    public bool IsRecent { get; init; }

    /// <summary>Extra detail shown as a hover tooltip (e.g. email/phone/notes, provider).</summary>
    public string? TooltipText { get; init; }
}

internal static class EntityRefMentionSupport
{
    /// <summary>Rows shown before the "+N more, keep typing" footer kicks in.</summary>
    public const int MaxVisibleOptions = 6;

    public static IReadOnlyList<EntityRefs.Kind> DefaultEnabledKinds { get; } =
    [
        EntityRefs.Kind.Contact,
        EntityRefs.Kind.Mailbox
    ];

    public static string KindLabel(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "Contact",
        EntityRefs.Kind.Mailbox => "Mailbox",
        EntityRefs.Kind.Tag => "Tag",
        EntityRefs.Kind.Bucket => "Bucket",
        _ => kind.ToString()
    };

    public static string KindPrefix(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "contact",
        EntityRefs.Kind.Mailbox => "mailbox",
        EntityRefs.Kind.Tag => "tag",
        EntityRefs.Kind.Bucket => "bucket",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static MentionContext? TryGetContext(
        string? text,
        int caret,
        IReadOnlyList<EntityRefs.Kind> enabledKinds)
    {
        if (string.IsNullOrEmpty(text) || caret <= 0 || caret > text.Length)
        {
            return null;
        }

        var at = -1;
        for (var i = caret - 1; i >= 0; i--)
        {
            if (text[i] == '@')
            {
                if (i == 0 || char.IsWhiteSpace(text[i - 1]))
                {
                    at = i;
                }

                break;
            }

            if (char.IsWhiteSpace(text[i]))
            {
                break;
            }
        }

        if (at < 0)
        {
            return null;
        }

        var segment = text[at..caret];
        if (segment.Length <= 1)
        {
            return new MentionContext
            {
                Step = MentionPickerStep.Kind,
                MentionStart = at,
                Caret = caret,
                Query = string.Empty
            };
        }

        var body = segment[1..];
        if (body.Any(char.IsWhiteSpace))
        {
            return null;
        }

        var colon = body.IndexOf(EntityRefs.Separator);
        if (colon < 0)
        {
            if (!IsPartialKindPrefix(body, enabledKinds))
            {
                return null;
            }

            return new MentionContext
            {
                Step = MentionPickerStep.Kind,
                MentionStart = at,
                Caret = caret,
                Query = body
            };
        }

        var kindPrefix = body[..colon];
        if (!TryMatchKind(kindPrefix, enabledKinds, out var kind))
        {
            return null;
        }

        var aliasQuery = body[(colon + 1)..];
        return new MentionContext
        {
            Step = MentionPickerStep.Alias,
            MentionStart = at,
            Caret = caret,
            Query = aliasQuery,
            Kind = kind
        };
    }

    public static IReadOnlyList<EntityRefMentionOption> BuildKindOptions(
        IReadOnlyList<EntityRefs.Kind> enabledKinds,
        string query)
    {
        return enabledKinds
            .Where(kind => MatchesQuery(KindPrefix(kind), query))
            .OrderBy(kind => Rank(KindPrefix(kind), KindLabel(kind), null, query))
            .ThenBy(kind => KindLabel(kind), StringComparer.OrdinalIgnoreCase)
            .Select(kind => new EntityRefMentionOption
            {
                OptionKind = MentionOptionKind.Kind,
                EntityKind = kind,
                PrimaryLabel = KindLabel(kind),
                SecondaryLabel = $"@{KindPrefix(kind)}:",
                InsertText = $"@{KindPrefix(kind)}{EntityRefs.Separator}",
                ClosesPicker = false
            })
            .ToList();
    }

    public static IReadOnlyList<EntityRefMentionOption> BuildAliasOptions(
        EntityRefs.Kind kind,
        string query,
        IReadOnlyList<ContactSummaryDto> contacts,
        IReadOnlyList<EmailAccountSummaryDto> mailboxes,
        IReadOnlyList<string>? recentHandles = null)
    {
        recentHandles ??= [];

        IEnumerable<EntityRefMentionOption> options = kind switch
        {
            EntityRefs.Kind.Contact => contacts
                .Where(contact => MatchesAliasQuery(contact.Alias, contact.ListLabel, contact.Email, query))
                .OrderBy(contact => Rank(contact.Alias, contact.ListLabel, contact.Email, query))
                .ThenBy(contact => contact.ListLabel, StringComparer.OrdinalIgnoreCase)
                .Select(contact =>
                {
                    var handle = EntityRefs.Format(EntityRefs.Kind.Contact, contact.Alias);
                    return new EntityRefMentionOption
                    {
                        OptionKind = MentionOptionKind.Alias,
                        EntityKind = EntityRefs.Kind.Contact,
                        PrimaryLabel = contact.ListLabel,
                        SecondaryLabel = $"@{contact.Alias}",
                        InsertText = $"@{handle}",
                        ClosesPicker = true,
                        AvatarText = ComputeInitials(contact.ListLabel),
                        IsRecent = RecentRank(handle, recentHandles) >= 0,
                        TooltipText = BuildContactTooltip(contact)
                    };
                }),
            EntityRefs.Kind.Mailbox => mailboxes
                .Where(account => MatchesAliasQuery(account.Alias, account.EmailAddress, account.ProviderName, query))
                .OrderBy(account => Rank(account.Alias, account.EmailAddress, account.ProviderName, query))
                .ThenBy(account => account.Alias, StringComparer.OrdinalIgnoreCase)
                .Select(account =>
                {
                    var handle = EntityRefs.Format(EntityRefs.Kind.Mailbox, account.Alias);
                    return new EntityRefMentionOption
                    {
                        OptionKind = MentionOptionKind.Alias,
                        EntityKind = EntityRefs.Kind.Mailbox,
                        PrimaryLabel = account.Alias,
                        SecondaryLabel = account.EmailAddress,
                        InsertText = $"@{handle}",
                        ClosesPicker = true,
                        IsRecent = RecentRank(handle, recentHandles) >= 0,
                        TooltipText = BuildMailboxTooltip(account)
                    };
                }),
            _ => []
        };

        var list = options.ToList();

        if (string.IsNullOrEmpty(query) && recentHandles.Count > 0)
        {
            // Recently used entities float to the top, most-recent first, when the user hasn't typed a query yet.
            list = list
                .OrderBy(option => RecentSortKey(option, recentHandles))
                .ToList();
        }

        return list;
    }

    private static int RecentSortKey(EntityRefMentionOption option, IReadOnlyList<string> recentHandles)
    {
        if (option.EntityKind is null)
        {
            return int.MaxValue;
        }

        var handle = ExtractHandle(option.InsertText);
        var index = RecentRank(handle, recentHandles);
        return index < 0 ? recentHandles.Count : index;
    }

    private static string ExtractHandle(string insertText) => insertText.TrimStart('@').TrimEnd(' ');

    private static int RecentRank(string handle, IReadOnlyList<string> recentHandles)
    {
        for (var i = 0; i < recentHandles.Count; i++)
        {
            if (string.Equals(recentHandles[i], handle, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Lower rank sorts first: prefix matches beat mid-string matches beat fuzzy matches.</summary>
    private static int Rank(string alias, string primary, string? secondary, string query)
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

    private static string BuildContactTooltip(ContactSummaryDto contact)
    {
        var parts = new List<string> { contact.ListLabel };

        if (!string.IsNullOrWhiteSpace(contact.Email))
        {
            parts.Add(contact.Email);
        }

        if (!string.IsNullOrWhiteSpace(contact.Phone))
        {
            parts.Add(contact.Phone);
        }

        return string.Join(" · ", parts);
    }

    private static string BuildMailboxTooltip(EmailAccountSummaryDto account)
    {
        var parts = new List<string> { account.EmailAddress, account.ProviderName };

        if (account.IsDefault)
        {
            parts.Add("default");
        }

        return string.Join(" · ", parts);
    }

    private static string ComputeInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static bool MatchesAliasQuery(
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

    private static bool MatchesQuery(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Loose subsequence match, e.g. "mnshptl" matches "Manish Patel". Only kicks in as a fallback
    /// once substring matching fails, and only for queries with enough signal to avoid noisy matches.
    /// </summary>
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

    private static bool IsPartialKindPrefix(string prefix, IReadOnlyList<EntityRefs.Kind> enabledKinds)
    {
        if (prefix.Length == 0)
        {
            return true;
        }

        return enabledKinds.Any(kind => KindPrefix(kind).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryMatchKind(
        string prefix,
        IReadOnlyList<EntityRefs.Kind> enabledKinds,
        out EntityRefs.Kind kind)
    {
        foreach (var candidate in enabledKinds)
        {
            if (KindPrefix(candidate).Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                kind = candidate;
                return true;
            }
        }

        kind = default;
        return false;
    }
}
