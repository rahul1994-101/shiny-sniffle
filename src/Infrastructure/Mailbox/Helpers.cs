using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using System.Net;
using System.Text.RegularExpressions;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Mailbox;

#region # Connection

internal static class MailboxConnectionHelpers
{
    internal static Task<T> ExecuteImapAsync<T>(EmailSettings config, Func<ImapClient, CancellationToken, Task<T>> execute, CancellationToken cancellationToken) =>
        ExecuteAsync(config, cancellationToken, smtp: false, execute);

    internal static Task<T> ExecuteSmtpAsync<T>(EmailSettings config, Func<SmtpClient, CancellationToken, Task<T>> execute, CancellationToken cancellationToken) =>
        ExecuteAsync(config, cancellationToken, smtp: true, execute);

    internal static async Task<ConnectionProbeResult> ProbeConnectionsAsync(EmailSettings config, CancellationToken cancellationToken)
    {
        var imap = await TrySessionAsync<ImapClient>(config, cancellationToken, smtp: false);
        var smtp = await TrySessionAsync<SmtpClient>(config, cancellationToken, smtp: true);
        return new ConnectionProbeResult(imap.Ok, smtp.Ok, imap.Error, smtp.Error);
    }

    internal static string FormatConnectionProbeMessage(ConnectionProbeResult probe)
    {
        var parts = new List<string>();
        if (probe.ImapOk)
        {
            parts.Add("IMAP OK");
        }
        else
        {
            parts.Add($"IMAP failed: {probe.ImapError}");
        }

        if (probe.SmtpOk)
        {
            parts.Add("SMTP OK");
        }
        else
        {
            parts.Add($"SMTP failed: {probe.SmtpError}");
        }

        return string.Join(" · ", parts);
    }

    internal static string FriendlyError(Exception ex) =>
        ex switch
        {
            AuthenticationException => "Authentication failed. Check username and password.",
            SslHandshakeException => "TLS/SSL connection failed. Check ports and SSL settings.",
            ServiceNotConnectedException => "Could not connect to the mail server.",
            SmtpCommandException smtp when smtp.Message.Contains("STARTTLS", StringComparison.OrdinalIgnoreCase) =>
                "SMTP requires STARTTLS. Try port 587 with SMTP SSL enabled.",
            _ => ex.Message
        };

    internal sealed record ConnectionProbeResult(
        bool ImapOk,
        bool SmtpOk,
        string? ImapError,
        string? SmtpError);

    #region # Private Helpers

    private const int ConnectTimeoutMs = 30_000;

    private static async Task<T> ExecuteAsync<TClient, T>(EmailSettings config, CancellationToken cancellationToken, bool smtp, Func<TClient, CancellationToken, Task<T>> work) where TClient : MailService, new()
    {
        using var client = CreateClient<TClient>();
        var (host, port, secure) = GetEndpoint(config, smtp);
        await client.ConnectAsync(host, port, secure, cancellationToken);
        await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        try
        {
            return await work(client, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    private static async Task<(bool Ok, string? Error)> TrySessionAsync<TClient>(EmailSettings config, CancellationToken cancellationToken, bool smtp) where TClient : MailService, new()
    {
        try
        {
            using var client = CreateClient<TClient>();
            var (host, port, secure) = GetEndpoint(config, smtp);
            await client.ConnectAsync(host, port, secure, cancellationToken);
            await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, FriendlyError(ex));
        }
    }

    private static (string Host, int Port, SecureSocketOptions Secure) GetEndpoint(EmailSettings config, bool smtp) =>
        smtp
            ? (config.SmtpHost, config.SmtpPort, GetSecureSocketOptions(config.SmtpPort, config.SmtpUseSsl, smtp: true))
            : (config.ImapHost, config.ImapPort, GetSecureSocketOptions(config.ImapPort, config.ImapUseSsl, smtp: false));

    private static SecureSocketOptions GetSecureSocketOptions(int port, bool useSsl, bool smtp)
    {
        if (!useSsl)
        {
            return SecureSocketOptions.None;
        }

        return smtp
            ? port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.Auto
            }
            : port switch
            {
                143 => SecureSocketOptions.StartTls,
                993 => SecureSocketOptions.SslOnConnect,
                _ => SecureSocketOptions.Auto
            };
    }

    private static T CreateClient<T>() where T : MailService, new() =>
        new()
        {
            Timeout = ConnectTimeoutMs,
            // Intentional v1 tradeoff: many hosted mail servers use misconfigured or private CAs;
            // disabling revocation checks avoids false negatives on connect. Revisit if exposing a strict-TLS option.
            CheckCertificateRevocation = false
        };

    #endregion
}

#endregion

#region # Queries

/// <summary>Canonical mailbox read limits — enforced in <see cref="MailKitMailboxService"/>.</summary>
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
            $"Folder '{trimmed}' was not found. List mailbox folders to see available names.");
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
            "Trash folder was not found on this mailbox.");
    }

    internal static bool IsTrashFolder(IMailFolder folder) =>
        folder.Attributes.HasFlag(FolderAttributes.Trash);

    internal static string? NormalizeFolderKey(string? folder) =>
        IsInboxAlias(folder) ? null : string.IsNullOrWhiteSpace(folder) ? null : folder.Trim();

    internal static IEnumerable<IGrouping<string?, MessageKey>> GroupMessagesByFolder(IReadOnlyList<MessageKey> messages) =>
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

    internal static async Task CollectFoldersAsync(IMailFolder folder, List<FolderInfo> folders, CancellationToken cancellationToken)
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

    internal static FolderInfo MapFolder(IMailFolder folder) =>
        new()
        {
            Name = folder.Name,
            FullName = folder.FullName,
            Role = DescribeRole(folder)
        };

    internal static async Task<IReadOnlyList<FolderInfo>> ListAllAsync(ImapClient imap, CancellationToken cancellationToken)
    {
        var folders = new List<FolderInfo>();

        if (imap.Inbox is not null)
        {
            folders.Add(MapFolder(imap.Inbox));
        }

        foreach (var ns in imap.PersonalNamespaces)
        {
            var root = imap.GetFolder(ns);
            await CollectFoldersAsync(root, folders, cancellationToken);
        }

        return folders
            .GroupBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
        foreach (var ns in imap.PersonalNamespaces)
        {
            var root = imap.GetFolder(ns);
            var match = await FindFolderByNameAsync(root, name, cancellationToken);
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

internal static class MailboxSearchHelpers
{
    internal static SearchQuery BuildQuery(ListMessagesFilters query)
    {
        SearchQuery search = query.SinceUtc is null
            ? SearchQuery.All
            : SearchQuery.DeliveredAfter(query.SinceUtc.Value);

        if (query.UntilUtcExclusive is not null)
        {
            search = search.And(SearchQuery.DeliveredBefore(query.UntilUtcExclusive.Value));
        }

        if (query.UnreadOnly)
        {
            search = search.And(SearchQuery.NotSeen);
        }

        if (!string.IsNullOrWhiteSpace(query.FromContains))
        {
            search = search.And(SearchQuery.FromContains(query.FromContains.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(query.SubjectContains))
        {
            search = search.And(SearchQuery.SubjectContains(query.SubjectContains.Trim()));
        }

        return search;
    }
}

internal static class MailboxSummaryHelpers
{
    private const MessageSummaryItems ListFetchItems =
        MessageSummaryItems.UniqueId
        | MessageSummaryItems.Envelope
        | MessageSummaryItems.Flags
        | MessageSummaryItems.PreviewText;

    internal static async Task<ListMessagesResult> ListAsync(ImapClient imap, ListMessagesFilters query, CancellationToken cancellationToken)
    {
        var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, query.Folder, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        return await ListAsync(folder, query, cancellationToken);
    }

    internal static async Task<ListMessagesResult> ListAsync(IMailFolder folder, ListMessagesFilters query, CancellationToken cancellationToken)
    {
        var search = MailboxSearchHelpers.BuildQuery(query);
        var ids = await folder.SearchAsync(search, cancellationToken);
        var totalMatched = ids.Count;

        if (query.CountOnly)
        {
            return new ListMessagesResult { TotalMatched = totalMatched };
        }

        if (ids.Count == 0)
        {
            return new ListMessagesResult { TotalMatched = 0 };
        }

        var limit = MailboxReadLimits.ClampListLimit(query.Limit);
        var selected = ids.TakeLast(limit).Reverse().ToList();
        var fetched = await folder.FetchAsync(selected, ListFetchItems, cancellationToken);
        var byUid = fetched.ToDictionary(s => s.UniqueId, s => s);

        var summaries = new List<MessageSummary>(selected.Count);
        foreach (var id in selected)
        {
            if (!byUid.TryGetValue(id, out var summary))
            {
                continue;
            }

            summaries.Add(MapSummary(summary));
        }

        return new ListMessagesResult
        {
            Messages = summaries,
            TotalMatched = totalMatched
        };
    }

    private static MessageSummary MapSummary(IMessageSummary summary)
    {
        var envelope = summary.Envelope;

        return new MessageSummary
        {
            Uid = summary.UniqueId.Id,
            From = envelope?.From?.ToString() ?? "(unknown)",
            Subject = string.IsNullOrWhiteSpace(envelope?.Subject) ? "(no subject)" : envelope.Subject,
            Date = envelope?.Date ?? DateTimeOffset.MinValue,
            IsUnread = IsUnread(summary.Flags),
            Snippet = FormatSnippet(summary.PreviewText)
        };
    }

    private static bool IsUnread(MessageFlags? flags) =>
        flags is null || !flags.Value.HasFlag(MessageFlags.Seen);

    private static string? FormatSnippet(string? preview)
    {
        if (string.IsNullOrWhiteSpace(preview))
        {
            return null;
        }

        var text = preview.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= MailboxReadLimits.SnippetMaxLength
            ? text
            : text[..MailboxReadLimits.SnippetMaxLength] + "…";
    }
}

internal static class MailboxMessageHelpers
{
    private const MessageSummaryItems DetailFlagItems =
        MessageSummaryItems.UniqueId | MessageSummaryItems.Flags;

    internal static async Task<IReadOnlyList<MessageDetail>> GetDetailsAsync(IMailFolder folder, IReadOnlyList<uint> uids, CancellationToken cancellationToken)
    {
        if (uids.Count == 0)
        {
            return [];
        }

        var uniqueIds = uids.Select(uid => new UniqueId(uid)).ToList();
        var summaries = await folder.FetchAsync(uniqueIds, DetailFlagItems, cancellationToken);
        var flagsByUid = summaries.ToDictionary(s => s.UniqueId.Id, s => s.Flags);

        var details = new List<MessageDetail>(uids.Count);
        foreach (var uid in uids)
        {
            if (!flagsByUid.ContainsKey(uid))
            {
                continue;
            }

            var message = await folder.GetMessageAsync(new UniqueId(uid), cancellationToken);
            flagsByUid.TryGetValue(uid, out var flags);
            details.Add(MapDetail(message, folder.FullName, uid, flags));
        }

        return details;
    }

    internal static async Task<IReadOnlyList<MessageDetail>> GetManyAsync(ImapClient imap, IReadOnlyList<MessageKey> messages, CancellationToken cancellationToken)
    {
        var found = new Dictionary<MessageLookupKey, MessageDetail>(MessageLookupKey.Comparer);

        foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var uids = group.Select(m => m.Uid).Distinct().ToList();
            var folderDetails = await GetDetailsAsync(folder, uids, cancellationToken);
            var detailsByUid = folderDetails.ToDictionary(d => d.Uid);

            foreach (var message in group)
            {
                if (detailsByUid.TryGetValue(message.Uid, out var detail))
                {
                    found[new MessageLookupKey(message)] = detail;
                }
            }
        }

        var ordered = new List<MessageDetail>(messages.Count);
        foreach (var message in messages)
        {
            if (found.TryGetValue(new MessageLookupKey(message), out var detail))
            {
                ordered.Add(detail);
            }
        }

        return ordered;
    }

    private static MessageDetail MapDetail(MimeMessage message, string folderName, uint uid, MessageFlags? flags)
    {
        var body = EmailMessageBodyHelpers.GetPlainBody(message);

        return new MessageDetail
        {
            Uid = uid,
            From = message.From?.ToString() ?? "(unknown)",
            Subject = string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject,
            Date = message.Date,
            Body = body.Text,
            Folder = folderName,
            BodyFromHtml = body.FromHtml,
            IsUnread = flags is null || !flags.Value.HasFlag(MessageFlags.Seen),
            AttachmentNames = EmailMessageBodyHelpers.GetAttachmentNames(message)
        };
    }

    private readonly record struct MessageLookupKey(string? Folder, uint Uid)
    {
        internal MessageLookupKey(MessageKey message)
            : this(MailboxFolderResolverHelpers.NormalizeFolderKey(message.Folder), message.Uid)
        {
        }

        internal static IEqualityComparer<MessageLookupKey> Comparer { get; } =
            EqualityComparer<MessageLookupKey>.Create(
                (left, right) =>
                    left.Uid == right.Uid &&
                    string.Equals(left.Folder, right.Folder, StringComparison.OrdinalIgnoreCase),
                key => HashCode.Combine(
                    key.Uid,
                    key.Folder is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(key.Folder)));
    }
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

#endregion

#region # Commands

internal static class MailboxCommandsHelpers
{
    internal static async Task<MailboxCommandResult> DeleteAsync(ImapClient imap, IReadOnlyList<MessageKey> messages, CancellationToken cancellationToken)
    {
        var affected = 0;

        foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
        {
            var sourceFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
            await sourceFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
            if (uids.Count == 0)
            {
                continue;
            }

            if (MailboxFolderResolverHelpers.IsTrashFolder(sourceFolder))
            {
                await sourceFolder.AddFlagsAsync(uids, MessageFlags.Deleted, silent: true, cancellationToken);
                await sourceFolder.ExpungeAsync(uids, cancellationToken);
            }
            else
            {
                var trash = await MailboxFolderResolverHelpers.GetTrashFolderAsync(imap, cancellationToken);
                await sourceFolder.MoveToAsync(uids, trash, cancellationToken);
            }

            affected += uids.Count;
        }

        return new MailboxCommandResult
        {
            Success = true,
            AffectedCount = affected,
            Message = affected == 1
                ? "Moved 1 message to trash."
                : $"Moved {affected} messages to trash."
        };
    }

    internal static async Task<MailboxCommandResult> MoveAsync(ImapClient imap, IReadOnlyList<MessageKey> messages, string destinationFolder, CancellationToken cancellationToken)
    {
        var destination = await MailboxFolderResolverHelpers.GetFolderAsync(imap, destinationFolder, cancellationToken);
        var affected = 0;

        foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
        {
            var sourceFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
            await sourceFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
            if (uids.Count == 0)
            {
                continue;
            }

            await sourceFolder.MoveToAsync(uids, destination, cancellationToken);
            affected += uids.Count;
        }

        return new MailboxCommandResult
        {
            Success = true,
            AffectedCount = affected,
            Message = affected == 1
                ? $"Moved 1 message to '{destination.FullName}'."
                : $"Moved {affected} messages to '{destination.FullName}'."
        };
    }

    internal static async Task<MailboxCommandResult> SetFlagsAsync(ImapClient imap, IReadOnlyList<MessageKey> messages, MessageFlagAction flag, CancellationToken cancellationToken)
    {
        var affected = 0;

        foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
            if (uids.Count == 0)
            {
                continue;
            }

            switch (flag)
            {
                case MessageFlagAction.Read:
                    await folder.AddFlagsAsync(uids, MessageFlags.Seen, silent: true, cancellationToken);
                    break;
                case MessageFlagAction.Unread:
                    await folder.RemoveFlagsAsync(uids, MessageFlags.Seen, silent: true, cancellationToken);
                    break;
                case MessageFlagAction.Flagged:
                    await folder.AddFlagsAsync(uids, MessageFlags.Flagged, silent: true, cancellationToken);
                    break;
                case MessageFlagAction.Unflagged:
                    await folder.RemoveFlagsAsync(uids, MessageFlags.Flagged, silent: true, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, "Unsupported message flag action.");
            }

            affected += uids.Count;
        }

        var actionLabel = flag switch
        {
            MessageFlagAction.Read => "marked read",
            MessageFlagAction.Unread => "marked unread",
            MessageFlagAction.Flagged => "flagged",
            MessageFlagAction.Unflagged => "unflagged",
            _ => "updated"
        };

        return new MailboxCommandResult
        {
            Success = true,
            AffectedCount = affected,
            Message = affected == 1
                ? $"1 message {actionLabel}."
                : $"{affected} messages {actionLabel}."
        };
    }
}

internal static class MailboxSendHelpers
{
    internal static async Task<SendMailResult> SendAsync(SmtpClient smtp, EmailSettings config, OutboundMail mail, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.EmailAddress, config.EmailAddress));
        message.To.Add(MailboxAddress.Parse(mail.To));
        message.Subject = mail.Subject;
        message.Body = new TextPart("plain") { Text = mail.Body };

        await smtp.SendAsync(message, cancellationToken);

        return new SendMailResult
        {
            Success = true,
            Message = $"Email sent to {mail.To}."
        };
    }
}

#endregion
