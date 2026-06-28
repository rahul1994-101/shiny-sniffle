using MailKit;
using MailKit.Net.Imap;

namespace Infrastructure.Utilities.Helpers;

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
}
