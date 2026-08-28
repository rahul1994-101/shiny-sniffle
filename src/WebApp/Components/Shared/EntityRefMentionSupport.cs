using Application.Features.Shared;

namespace WebApp.Components.Shared;

internal enum MentionPickerStep
{
    None,
    Kind,
    Alias,
    Global
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
        EntityRefs.Kind.Mailbox,
        EntityRefs.Kind.Tag,
        EntityRefs.Kind.Bucket
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

    public static string GetEmptyWorkspaceMessage(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "No contacts yet. Add someone your agents should recognize.",
        EntityRefs.Kind.Mailbox => "No mailboxes yet. Connect an inbox to get started.",
        EntityRefs.Kind.Tag => "No tags yet. Create labels for facets, roles, and topics.",
        EntityRefs.Kind.Bucket => "No buckets yet. Create groups like clients or family.",
        _ => $"No {KindLabel(kind).ToLowerInvariant()}s yet."
    };

    public static string GetWorkspaceHref(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "/workspace/contacts",
        EntityRefs.Kind.Mailbox => "/workspace/email/accounts",
        EntityRefs.Kind.Tag => "/workspace/tags",
        EntityRefs.Kind.Bucket => "/workspace/buckets",
        _ => "/workspace"
    };

    public static string GetWorkspaceLinkLabel(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "Open Workspace → Contacts",
        EntityRefs.Kind.Mailbox => "Open Workspace → Email accounts",
        EntityRefs.Kind.Tag => "Open Workspace → Tags",
        EntityRefs.Kind.Bucket => "Open Workspace → Buckets",
        _ => "Open Workspace"
    };

    public static string GetGlobalEmptyWorkspaceMessage() =>
        "Nothing to reference yet. Add contacts, mailboxes, tags, or buckets in Workspace.";

    public static string GetGlobalWorkspaceLinkLabel() => "Open Workspace";

    public static string KindCssSuffix(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "contact",
        EntityRefs.Kind.Mailbox => "mailbox",
        EntityRefs.Kind.Tag => "tag",
        EntityRefs.Kind.Bucket => "bucket",
        _ => "default"
    };

    public static string BuildMentionTitle(EntityRefs.Kind kind, string alias) =>
        $"{KindLabel(kind)} · {alias}";

    public static string BuildMentionAriaLabel(EntityRefs.Kind kind, string alias) =>
        $"{KindLabel(kind)}, {alias}";

    public static MentionContext? TryGetContext(
        string? text,
        int caret,
        IReadOnlyList<EntityRefs.Kind> enabledKinds)
    {
        if (string.IsNullOrEmpty(text) || caret <= 0 || caret > text.Length)
        {
            return null;
        }

        var triggerIndex = -1;
        var trigger = '\0';
        for (var i = caret - 1; i >= 0; i--)
        {
            if (text[i] is '@' or '/')
            {
                if (i == 0 || char.IsWhiteSpace(text[i - 1]))
                {
                    triggerIndex = i;
                    trigger = text[i];
                }

                break;
            }

            if (char.IsWhiteSpace(text[i]))
            {
                break;
            }
        }

        if (triggerIndex < 0)
        {
            return null;
        }

        if (trigger == '/')
        {
            var slashSegment = text[triggerIndex..caret];
            if (slashSegment.Length <= 1)
            {
                return new MentionContext
                {
                    Step = MentionPickerStep.Global,
                    MentionStart = triggerIndex,
                    Caret = caret,
                    Query = string.Empty
                };
            }

            var slashQuery = slashSegment[1..];
            if (slashQuery.Any(char.IsWhiteSpace))
            {
                return null;
            }

            return new MentionContext
            {
                Step = MentionPickerStep.Global,
                MentionStart = triggerIndex,
                Caret = caret,
                Query = slashQuery
            };
        }

        var at = triggerIndex;

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

    public static IReadOnlyList<EntityRefMentionOption> BuildAliasOptionsFromItems(
        EntityRefs.Kind kind,
        string query,
        IReadOnlyList<EntityRefMentionItemDto> items,
        IReadOnlyList<string>? recentHandles = null)
    {
        recentHandles ??= [];

        var list = items.Select(item =>
        {
            var handle = EntityRefs.Format(kind, item.Alias);
            return new EntityRefMentionOption
            {
                OptionKind = MentionOptionKind.Alias,
                EntityKind = kind,
                PrimaryLabel = item.PrimaryLabel,
                SecondaryLabel = item.SecondaryLabel,
                InsertText = $"@{handle}",
                ClosesPicker = true,
                AvatarText = item.AvatarText,
                IsRecent = RecentRank(handle, recentHandles) >= 0,
                TooltipText = item.TooltipText
            };
        }).ToList();

        if (string.IsNullOrEmpty(query) && recentHandles.Count > 0)
        {
            list = list
                .OrderBy(option => RecentSortKey(option, recentHandles))
                .ToList();
        }

        return list;
    }

    public static IReadOnlyList<EntityRefMentionOption> BuildGlobalOptionsFromItems(
        string query,
        IReadOnlyList<EntityRefMentionItemDto> items,
        IReadOnlyList<string>? recentHandles = null)
    {
        recentHandles ??= [];

        return items.Select(item =>
        {
            var handle = EntityRefs.Format(item.Kind, item.Alias);
            return new EntityRefMentionOption
            {
                OptionKind = MentionOptionKind.Alias,
                EntityKind = item.Kind,
                PrimaryLabel = item.PrimaryLabel,
                SecondaryLabel = $"@{handle}",
                InsertText = $"@{handle}",
                ClosesPicker = true,
                AvatarText = item.AvatarText,
                IsRecent = RecentRank(handle, recentHandles) >= 0,
                TooltipText = item.TooltipText
            };
        }).ToList();
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

    private static bool MatchesQuery(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

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
