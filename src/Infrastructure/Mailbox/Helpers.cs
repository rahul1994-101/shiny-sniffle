using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using MimeKit.Utils;
using System.Net;
using System.Text.RegularExpressions;

namespace Infrastructure.Mailbox;

/// <summary>Canonical mailbox read limits — enforced in <see cref="MailKitMailboxService"/>; referenced by Application tools/agent copy.</summary>
public static class MailboxReadLimits
{
    public const int DefaultListLimit = 20;

    public const int MinListLimit = 1;

    public const int MaxListLimit = 50;

    public const int SnippetMaxLength = 120;

    public const int MaxMessageBodyLength = 12_000;

    public const int MaxBatchGetCount = 5;

    public static int ClampListLimit(int limit) =>
        limit <= 0 ? DefaultListLimit : Math.Clamp(limit, MinListLimit, MaxListLimit);
}

internal static class MailboxFolderResolverHelpers
{
    internal static bool IsInboxAlias(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return true;
        }

        return folder.Trim().Equals("inbox", StringComparison.OrdinalIgnoreCase);
    }

    internal static async Task<IMailFolder> GetFolderAsync(ImapClient imap, string? folder, CancellationToken cancellationToken)
    {
        if (IsInboxAlias(folder))
        {
            return imap.Inbox ?? throw new InvalidOperationException("IMAP inbox folder is not available.");
        }

        var trimmed = folder!.Trim();

        if (TryMapSpecialFolder(trimmed, out var special))
        {
            return imap.GetFolder(special)
                ?? throw new InvalidOperationException($"Folder '{trimmed}' is not available on this mailbox.");
        }

        try
        {
            var byPath = await imap.GetFolderAsync(trimmed, cancellationToken);
            if (byPath.Exists)
            {
                return byPath;
            }
        }
        catch (FolderNotFoundException)
        {
        }

        var byName = await FindFolderByNameAsync(imap, trimmed, cancellationToken);
        if (byName is not null)
        {
            return byName;
        }

        throw new InvalidOperationException(
            $"Folder '{trimmed}' was not found. Call list_mailbox_folders for available folders.");
    }

    internal static async Task<IMailFolder> GetTrashFolderAsync(ImapClient imap, CancellationToken cancellationToken)
    {
        var trash = imap.GetFolder(SpecialFolder.Trash);
        if (trash is not null && trash.Exists)
        {
            return trash;
        }

        foreach (var name in new[] { "Trash", "Deleted", "Deleted Items", "Bin" })
        {
            var match = await FindFolderByNameAsync(imap, name, cancellationToken);
            if (match is not null)
            {
                return match;
            }
        }

        throw new InvalidOperationException(
            "Trash folder was not found. Call list_mailbox_folders for available folders.");
    }

    internal static bool IsTrashFolder(IMailFolder folder) =>
        folder.Attributes.HasFlag(FolderAttributes.Trash);

    internal static string? NormalizeFolderKey(string? folder) =>
        IsInboxAlias(folder) ? null : string.IsNullOrWhiteSpace(folder) ? null : folder.Trim();

    internal static IEnumerable<IGrouping<string?, MessageRef>> GroupMessagesByFolder(IReadOnlyList<MessageRef> messages) =>
        messages.GroupBy(
            m => NormalizeFolderKey(m.Folder),
            StringComparer.OrdinalIgnoreCase);

    internal static string? DescribeRole(IMailFolder folder)
    {
        var attributes = folder.Attributes;
        if (attributes.HasFlag(FolderAttributes.Inbox))
        {
            return "inbox";
        }

        if (attributes.HasFlag(FolderAttributes.Sent))
        {
            return "sent";
        }

        if (attributes.HasFlag(FolderAttributes.Drafts))
        {
            return "drafts";
        }

        if (attributes.HasFlag(FolderAttributes.Trash))
        {
            return "trash";
        }

        if (attributes.HasFlag(FolderAttributes.Junk))
        {
            return "junk";
        }

        if (attributes.HasFlag(FolderAttributes.Archive))
        {
            return "archive";
        }

        return null;
    }

    private static bool TryMapSpecialFolder(string folder, out SpecialFolder special)
    {
        special = default;
        var key = folder.ToLowerInvariant();

        switch (key)
        {
            case "sent":
            case "sent items":
            case "sent mail":
            case "sent messages":
                special = SpecialFolder.Sent;
                return true;
            case "draft":
            case "drafts":
                special = SpecialFolder.Drafts;
                return true;
            case "trash":
            case "deleted":
            case "deleted items":
            case "bin":
                special = SpecialFolder.Trash;
                return true;
            case "junk":
            case "spam":
                special = SpecialFolder.Junk;
                return true;
            case "archive":
            case "archives":
                special = SpecialFolder.Archive;
                return true;
            default:
                return false;
        }
    }

    private static async Task<IMailFolder?> FindFolderByNameAsync(ImapClient imap, string name, CancellationToken cancellationToken)
    {
        IMailFolder? match = null;

        foreach (var ns in imap.PersonalNamespaces)
        {
            var root = imap.GetFolder(ns);
            match = await FindFolderByNameAsync(root, name, cancellationToken);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static async Task<IMailFolder?> FindFolderByNameAsync(IMailFolder folder, string name, CancellationToken cancellationToken)
    {
        if (folder.Exists && folder.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return folder;
        }

        var children = await folder.GetSubfoldersAsync(false, cancellationToken);
        foreach (var child in children)
        {
            var match = await FindFolderByNameAsync(child, name, cancellationToken);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    internal static async Task CollectFoldersAsync(
        IMailFolder folder,
        List<MailboxFolderInfo> folders,
        CancellationToken cancellationToken)
    {
        if (!folder.Exists)
        {
            return;
        }

        folders.Add(MapFolder(folder));

        var children = await folder.GetSubfoldersAsync(false, cancellationToken);
        foreach (var child in children)
        {
            await CollectFoldersAsync(child, folders, cancellationToken);
        }
    }

    internal static MailboxFolderInfo MapFolder(IMailFolder folder) =>
        new()
        {
            Name = folder.Name,
            FullName = folder.FullName,
            Role = DescribeRole(folder)
        };
}

internal static partial class EmailMessageBodyHelpers
{
    [GeneratedRegex(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ScriptStyleBlockPattern();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BreakTagPattern();

    [GeneratedRegex(@"</p>", RegexOptions.IgnoreCase)]
    private static partial Regex ParagraphEndPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceCollapsePattern();

    internal sealed record PlainBodyResult(string Text, bool FromHtml);

    internal static PlainBodyResult GetPlainBody(MimeMessage message)
    {
        var text = GetPlainTextOrNull(message);
        if (text is not null)
        {
            return new PlainBodyResult(TruncateBody(text), FromHtml: false);
        }

        var html = message.HtmlBody;
        if (!string.IsNullOrWhiteSpace(html))
        {
            var converted = ConvertHtmlToPlainText(html);
            if (!string.IsNullOrWhiteSpace(converted))
            {
                return new PlainBodyResult(TruncateBody(converted), FromHtml: true);
            }
        }

        return new PlainBodyResult("(no readable body)", FromHtml: false);
    }

    internal static string? GetPlainTextOrNull(MimeMessage message)
    {
        var text = message.TextBody;
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    internal static IReadOnlyList<string> GetAttachmentNames(MimeMessage message)
    {
        var names = new List<string>();

        foreach (var entity in message.Attachments)
        {
            if (entity is not MimePart part)
            {
                continue;
            }

            var rawName = part.FileName
                ?? part.ContentDisposition?.FileName
                ?? part.ContentType.Name;

            if (string.IsNullOrWhiteSpace(rawName))
            {
                names.Add("(unnamed)");
                continue;
            }

            names.Add(MimeUtils.Unquote(rawName.Trim()));
        }

        return names;
    }

    private static string ConvertHtmlToPlainText(string html)
    {
        var stripped = ScriptStyleBlockPattern().Replace(html, " ");
        stripped = BreakTagPattern().Replace(stripped, "\n");
        stripped = ParagraphEndPattern().Replace(stripped, "\n");
        stripped = HtmlTagPattern().Replace(stripped, " ");
        stripped = WebUtility.HtmlDecode(stripped);
        return WhitespaceCollapsePattern().Replace(stripped, " ").Trim();
    }

    private static string TruncateBody(string text)
    {
        text = text.Trim();
        return text.Length <= MailboxReadLimits.MaxMessageBodyLength
            ? text
            : text[..MailboxReadLimits.MaxMessageBodyLength] + "… (truncated)";
    }
}
