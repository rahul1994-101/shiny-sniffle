using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Mailbox;

public sealed class MailKitMailboxService : IMailboxService
{
    public async Task<MailboxStatusResult> GetStatusAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        var probe = await ProbeConnectionsAsync(config, cancellationToken);

        if (probe.ImapOk && probe.SmtpOk)
        {
            return new MailboxStatusResult
            {
                IsConfigured = true,
                IsReachable = true,
                Message = "IMAP and SMTP are reachable."
            };
        }

        return new MailboxStatusResult
        {
            IsConfigured = true,
            IsReachable = probe.ImapOk,
            Message = FormatConnectionProbeMessage(probe)
        };
    }

    public async Task<MailboxTestResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        var probe = await ProbeConnectionsAsync(config, cancellationToken);

        if (probe.ImapOk && probe.SmtpOk)
        {
            return new MailboxTestResult
            {
                Success = true,
                ImapOk = true,
                SmtpOk = true,
                Message = "IMAP and SMTP connections succeeded."
            };
        }

        return new MailboxTestResult
        {
            Success = false,
            ImapOk = probe.ImapOk,
            SmtpOk = probe.SmtpOk,
            Message = FormatConnectionProbeMessage(probe)
        };
    }

    public async Task<InboxListResult> ListInboxAsync(EmailSettings config, InboxQuery query, CancellationToken cancellationToken = default)
    {
        using var imap = CreateImapClient();
        await ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, query.Folder, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var search = BuildInboxSearchQuery(query);
        var ids = await folder.SearchAsync(search, cancellationToken);
        var totalMatched = ids.Count;

        if (query.CountOnly)
        {
            await imap.DisconnectAsync(true, cancellationToken);
            return new InboxListResult { TotalMatched = totalMatched };
        }

        var limit = MailboxReadLimits.ClampListLimit(query.Limit);
        var selected = ids.TakeLast(limit).Reverse().ToList();
        var summaries = new List<InboxMessageSummary>(selected.Count);

        foreach (var id in selected)
        {
            var message = await folder.GetMessageAsync(id, cancellationToken);
            summaries.Add(new InboxMessageSummary
            {
                Uid = id.Id,
                From = message.From?.ToString() ?? "(unknown)",
                Subject = string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject,
                Date = message.Date,
                Snippet = BuildSnippet(message)
            });
        }

        await imap.DisconnectAsync(true, cancellationToken);
        return new InboxListResult
        {
            Messages = summaries,
            TotalMatched = totalMatched
        };
    }

    public async Task<InboxMessageDetail?> GetInboxMessageAsync(EmailSettings config, uint uid, string? folder = null, CancellationToken cancellationToken = default)
    {
        using var imap = CreateImapClient();
        await ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var mailFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, folder, cancellationToken);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var uniqueId = new UniqueId(uid);
        var matches = await mailFolder.SearchAsync(SearchQuery.Uids(new UniqueIdSet([uniqueId])), cancellationToken);
        if (matches.Count == 0)
        {
            await imap.DisconnectAsync(true, cancellationToken);
            return null;
        }

        var message = await mailFolder.GetMessageAsync(uniqueId, cancellationToken);
        var body = EmailMessageBodyHelpers.GetPlainBody(message);
        await imap.DisconnectAsync(true, cancellationToken);

        return new InboxMessageDetail
        {
            Uid = uid,
            From = message.From?.ToString() ?? "(unknown)",
            Subject = string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject,
            Date = message.Date,
            Body = body.Text,
            Folder = mailFolder.FullName,
            BodyFromHtml = body.FromHtml,
            AttachmentNames = EmailMessageBodyHelpers.GetAttachmentNames(message)
        };
    }

    public async Task<IReadOnlyList<MailboxFolderInfo>> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        using var imap = CreateImapClient();
        await ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var folders = new List<MailboxFolderInfo>();

        if (imap.Inbox is not null)
        {
            folders.Add(MapFolder(imap.Inbox));
        }

        foreach (var ns in imap.PersonalNamespaces)
        {
            var root = imap.GetFolder(ns);
            await CollectFoldersAsync(root, folders, cancellationToken);
        }

        await imap.DisconnectAsync(true, cancellationToken);

        return folders
            .GroupBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        if (!MailAddress.TryCreate(mail.To, out _))
        {
            return new SendMailResult
            {
                Success = false,
                Message = "Recipient email address is invalid."
            };
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.EmailAddress, config.EmailAddress));
        message.To.Add(MailboxAddress.Parse(mail.To));
        message.Subject = mail.Subject;
        message.Body = new TextPart("plain") { Text = mail.Body };

        using var smtp = CreateSmtpClient();
        await ConnectSmtpAsync(smtp, config, cancellationToken);
        await smtp.AuthenticateAsync(config.Username, config.Password, cancellationToken);
        await smtp.SendAsync(message, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);

        return new SendMailResult
        {
            Success = true,
            Message = $"Email sent to {mail.To}."
        };
    }

    #region # Private Helpers

    private const int ConnectTimeoutMs = 30_000;

    internal static SecureSocketOptions GetImapSecureSocketOptions(int port, bool useSsl)
    {
        if (!useSsl)
        {
            return SecureSocketOptions.None;
        }

        return port switch
        {
            143 => SecureSocketOptions.StartTls,
            993 => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.Auto
        };
    }

    internal static SecureSocketOptions GetSmtpSecureSocketOptions(int port, bool useSsl)
    {
        if (!useSsl)
        {
            return SecureSocketOptions.None;
        }

        return port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };
    }

    private static ImapClient CreateImapClient() =>
        new()
        {
            Timeout = ConnectTimeoutMs,
            // Intentional v1 tradeoff: many hosted mail servers use misconfigured or private CAs;
            // disabling revocation checks avoids false negatives on connect. Revisit if exposing a strict-TLS option.
            CheckCertificateRevocation = false
        };

    private static SmtpClient CreateSmtpClient() =>
        new()
        {
            Timeout = ConnectTimeoutMs,
            CheckCertificateRevocation = false
        };

    private async Task<ConnectionProbeResult> ProbeConnectionsAsync(
        EmailSettings config,
        CancellationToken cancellationToken)
    {
        var imapOk = false;
        var smtpOk = false;
        string? imapError = null;
        string? smtpError = null;

        try
        {
            using var imap = CreateImapClient();
            await ConnectImapAsync(imap, config, cancellationToken);
            await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            await imap.DisconnectAsync(true, cancellationToken);
            imapOk = true;
        }
        catch (Exception ex)
        {
            imapError = FriendlyError(ex);
        }

        try
        {
            using var smtp = CreateSmtpClient();
            await ConnectSmtpAsync(smtp, config, cancellationToken);
            await smtp.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
            smtpOk = true;
        }
        catch (Exception ex)
        {
            smtpError = FriendlyError(ex);
        }

        return new ConnectionProbeResult(imapOk, smtpOk, imapError, smtpError);
    }

    private static string FormatConnectionProbeMessage(ConnectionProbeResult probe)
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

    private sealed record ConnectionProbeResult(
        bool ImapOk,
        bool SmtpOk,
        string? ImapError,
        string? SmtpError);

    private static SearchQuery BuildInboxSearchQuery(InboxQuery query)
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

    private static async Task CollectFoldersAsync(IMailFolder folder, List<MailboxFolderInfo> folders, CancellationToken cancellationToken)
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

    private static MailboxFolderInfo MapFolder(IMailFolder folder) =>
        new()
        {
            Name = folder.Name,
            FullName = folder.FullName,
            Role = MailboxFolderResolverHelpers.DescribeRole(folder)
        };

    private static Task ConnectImapAsync(ImapClient client, EmailSettings config, CancellationToken cancellationToken)
    {
        var secure = GetImapSecureSocketOptions(config.ImapPort, config.ImapUseSsl);
        return client.ConnectAsync(config.ImapHost, config.ImapPort, secure, cancellationToken);
    }

    private static Task ConnectSmtpAsync(SmtpClient client, EmailSettings config, CancellationToken cancellationToken)
    {
        var secure = GetSmtpSecureSocketOptions(config.SmtpPort, config.SmtpUseSsl);
        return client.ConnectAsync(config.SmtpHost, config.SmtpPort, secure, cancellationToken);
    }

    private static string? BuildSnippet(MimeMessage message)
    {
        var text = EmailMessageBodyHelpers.GetPlainTextOrNull(message);
        if (text is null && !string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            text = EmailMessageBodyHelpers.GetPlainBody(message).Text;
            if (text == "(no readable body)")
            {
                return null;
            }
        }

        if (text is null)
        {
            return null;
        }

        text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= MailboxReadLimits.SnippetMaxLength
            ? text
            : text[..MailboxReadLimits.SnippetMaxLength] + "…";
    }

    private static string FriendlyError(Exception ex) =>
        ex switch
        {
            AuthenticationException => "Authentication failed. Check username and password.",
            SslHandshakeException => "TLS/SSL connection failed. Check ports and SSL settings.",
            ServiceNotConnectedException => "Could not connect to the mail server.",
            SmtpCommandException smtp when smtp.Message.Contains("STARTTLS", StringComparison.OrdinalIgnoreCase) =>
                "SMTP requires STARTTLS. Try port 587 with SMTP SSL enabled.",
            _ => ex.Message
        };

    #endregion
}
