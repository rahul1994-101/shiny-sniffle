using WebApp.Components.Shared;

namespace WebApp.Components.Pages.Help;

public sealed record HelpTopic(
    string Slug,
    string Title,
    string Lead,
    NavIconName Icon,
    string AppHref)
{
    public string NormalizeAppHref() => AppHref.TrimEnd('/').ToLowerInvariant();
}

public sealed record HelpTopicGroup(
    string? Label,
    IReadOnlyList<HelpTopic> Topics);

public sealed record HelpModule(
    string Slug,
    string Title,
    string Lead,
    NavIconName Icon,
    IReadOnlyList<HelpTopicGroup> Groups)
{
    public string Href => $"/help/{Slug}";

    public string TopicHref(HelpTopic topic) => $"/help/{Slug}/{topic.Slug}";
}

public static class HelpCatalog
{
    public static readonly HelpModule Settings = new(
        "settings",
        "Settings",
        "Account profile, mail presets, and device appearance.",
        NavIconName.Settings,
        [
            new HelpTopicGroup(
                "Account settings",
                [
                    new HelpTopic(
                        "general",
                        "General",
                        "Profile, mobile, and sign-in password.",
                        NavIconName.General,
                        "/settings/general"),
                    new HelpTopic(
                        "email-providers",
                        "Email providers",
                        "IMAP/SMTP server presets for mailbox connections.",
                        NavIconName.EmailProviders,
                        "/settings/email/providers")
                ]),
            new HelpTopicGroup(
                "This device",
                [
                    new HelpTopic(
                        "appearance",
                        "Appearance",
                        "Color theme for this device.",
                        NavIconName.Appearance,
                        "/settings/appearance")
                ])
        ]);

    public static readonly HelpModule Workspace = new(
        "workspace",
        "Workspace",
        "Connected inboxes, contacts, tags, and buckets.",
        NavIconName.Workspace,
        [
            new HelpTopicGroup(
                "Data",
                [
                    new HelpTopic(
                        "email-accounts",
                        "Email accounts",
                        "Connected inboxes for the Email agent.",
                        NavIconName.EmailAccounts,
                        "/workspace/email/accounts"),
                    new HelpTopic(
                        "contacts",
                        "Contacts",
                        "People you reference in rules and workflows.",
                        NavIconName.Contacts,
                        "/workspace/contacts")
                ]),
            new HelpTopicGroup(
                "Organization",
                [
                    new HelpTopic(
                        "buckets",
                        "Buckets",
                        "Named groups for contacts and mailboxes.",
                        NavIconName.Buckets,
                        "/workspace/buckets"),
                    new HelpTopic(
                        "tags",
                        "Tags",
                        "Labels for contacts and mailboxes.",
                        NavIconName.Tags,
                        "/workspace/tags")
                ])
        ]);

    public static readonly HelpModule Chat = new(
        "chat",
        "Chat",
        "Threads, agents, and triaging mail in conversation.",
        NavIconName.Chat,
        [
            new HelpTopicGroup(
                null,
                [
                    new HelpTopic(
                        "email-agent",
                        "Email agent",
                        "Use chat to triage and act on connected inboxes.",
                        NavIconName.Chat,
                        ""),
                    new HelpTopic(
                        "threads",
                        "Chat threads",
                        "Start, rename, and switch between conversations.",
                        NavIconName.Chat,
                        "")
                ])
        ]);

    public static readonly HelpModule Workflows = new(
        "workflows",
        "Workflows",
        "Automations and scheduled actions — coming later.",
        NavIconName.Workflows,
        [
            new HelpTopicGroup(
                null,
                [
                    new HelpTopic(
                        "overview",
                        "Overview",
                        "What workflows will do when the module ships.",
                        NavIconName.Workflows,
                        "/workflows")
                ])
        ]);

    public static readonly IReadOnlyList<HelpModule> PrimaryModules =
    [
        Workflows,
        Workspace,
        Settings
    ];

    public static readonly IReadOnlyList<HelpModule> Modules =
    [
        ..PrimaryModules,
        Chat
    ];

    public static HelpModule? TryGetModule(string? moduleSlug) =>
        string.IsNullOrWhiteSpace(moduleSlug)
            ? null
            : Modules.FirstOrDefault(m => string.Equals(m.Slug, moduleSlug, StringComparison.OrdinalIgnoreCase));

    public static HelpTopic? TryGetTopic(HelpModule module, string? topicSlug)
    {
        if (string.IsNullOrWhiteSpace(topicSlug))
        {
            return null;
        }

        foreach (var group in module.Groups)
        {
            var topic = group.Topics.FirstOrDefault(t =>
                string.Equals(t.Slug, topicSlug, StringComparison.OrdinalIgnoreCase));

            if (topic is not null)
            {
                return topic;
            }
        }

        return null;
    }

    public static (HelpModule Module, HelpTopic Topic)? TryResolveTopic(string? moduleSlug, string? topicSlug)
    {
        var module = TryGetModule(moduleSlug);
        if (module is null)
        {
            return null;
        }

        var topic = TryGetTopic(module, topicSlug);
        return topic is null ? null : (module, topic);
    }

    public static (HelpModule Module, HelpTopic Topic)? TryResolveByAppHref(string? appHref)
    {
        if (string.IsNullOrWhiteSpace(appHref))
        {
            return null;
        }

        var normalized = appHref.TrimEnd('/').ToLowerInvariant();
        foreach (var module in Modules)
        {
            foreach (var group in module.Groups)
            {
                foreach (var topic in group.Topics)
                {
                    if (string.Equals(topic.NormalizeAppHref(), normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        return (module, topic);
                    }
                }
            }
        }

        return null;
    }

    public static HelpTopic RequireTopic(HelpModule module, string topicSlug) =>
        TryGetTopic(module, topicSlug)
        ?? throw new InvalidOperationException($"Help topic '{topicSlug}' was not found in module '{module.Slug}'.");
}
