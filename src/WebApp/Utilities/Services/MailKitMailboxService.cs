using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;

using MimeKit;
using System.Net.Mail;
using WebApp.Models;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace WebApp.Utilities.Services;

public sealed class MailKitMailboxService : IMailboxService
{
    public async Task<MailboxStatusResult> GetStatusAsync(MailboxConnectionOptions config, CancellationToken cancellationToken = default)
    {
        try
        {
            using var imap = CreateImapClient();
            await ConnectImapAsync(imap, config, cancellationToken);
            await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            await imap.DisconnectAsync(true, cancellationToken);

            return new MailboxStatusResult
            {
                IsConfigured = true,
                IsReachable = true,
                Message = "Mailbox is configured and reachable."
            };
        }
        catch (Exception ex)
        {
            return new MailboxStatusResult
            {
                IsConfigured = true,
                IsReachable = false,
                Message = FriendlyError(ex)
            };
        }
    }

    public async Task<MailboxTestResult> TestConnectionAsync(MailboxConnectionOptions config, CancellationToken cancellationToken = default)
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

        if (imapOk && smtpOk)
        {
            return new MailboxTestResult
            {
                Success = true,
                ImapOk = true,
                SmtpOk = true,
                Message = "IMAP and SMTP connections succeeded."
            };
        }

        var parts = new List<string>();
        if (imapOk)
        {
            parts.Add("IMAP OK");
        }
        else
        {
            parts.Add($"IMAP failed: {imapError}");
        }

        if (smtpOk)
        {
            parts.Add("SMTP OK");
        }
        else
        {
            parts.Add($"SMTP failed: {smtpError}");
        }

        return new MailboxTestResult
        {
            Success = false,
            ImapOk = imapOk,
            SmtpOk = smtpOk,
            Message = string.Join(" · ", parts)
        };
    }

    public async Task<IReadOnlyList<InboxMessageSummary>> ListInboxAsync(MailboxConnectionOptions config, InboxQuery query, CancellationToken cancellationToken = default)
    {
        using var imap = CreateImapClient();
        await ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var inbox = imap.Inbox ?? throw new InvalidOperationException("IMAP inbox folder is not available.");
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var limit = Math.Clamp(query.Limit, 1, 50);
        IList<UniqueId> ids;

        if (query.SinceUtc is null)
        {
            ids = await inbox.SearchAsync(SearchQuery.All, cancellationToken);
        }
        else
        {
            ids = await inbox.SearchAsync(
                SearchQuery.DeliveredAfter(query.SinceUtc.Value),
                cancellationToken);
        }

        var selected = ids.TakeLast(limit).Reverse().ToList();
        var summaries = new List<InboxMessageSummary>(selected.Count);

        foreach (var id in selected)
        {
            var message = await inbox.GetMessageAsync(id, cancellationToken);
            summaries.Add(new InboxMessageSummary
            {
                From = message.From?.ToString() ?? "(unknown)",
                Subject = string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject,
                Date = message.Date,
                Snippet = BuildSnippet(message)
            });
        }

        await imap.DisconnectAsync(true, cancellationToken);
        return summaries;
    }

    public async Task<SendMailResult> SendAsync(MailboxConnectionOptions config, OutboundMail mail, CancellationToken cancellationToken = default)
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

    private static ImapClient CreateImapClient()
    {
        var client = new ImapClient
        {
            Timeout = ConnectTimeoutMs,
            CheckCertificateRevocation = false
        };

        return client;
    }

    private static SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient
        {
            Timeout = ConnectTimeoutMs,
            CheckCertificateRevocation = false
        };

        return client;
    }

    private static Task ConnectImapAsync(ImapClient client, MailboxConnectionOptions config, CancellationToken cancellationToken)
    {
        var secure = GetImapSecureSocketOptions(config.ImapPort, config.ImapUseSsl);
        return client.ConnectAsync(config.ImapHost, config.ImapPort, secure, cancellationToken);
    }

    private static Task ConnectSmtpAsync(SmtpClient client, MailboxConnectionOptions config, CancellationToken cancellationToken)
    {
        var secure = GetSmtpSecureSocketOptions(config.SmtpPort, config.SmtpUseSsl);
        return client.ConnectAsync(config.SmtpHost, config.SmtpPort, secure, cancellationToken);
    }

    private static string? BuildSnippet(MimeMessage message)
    {
        var text = message.TextBody;
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= 120 ? text : text[..120] + "…";
    }

    private static string FriendlyError(Exception ex) =>
        ex switch
        {
            MailKit.Security.AuthenticationException => "Authentication failed. Check username and password.",
            SslHandshakeException => "TLS/SSL connection failed. Check ports and SSL settings.",
            ServiceNotConnectedException => "Could not connect to the mail server.",
            SmtpCommandException smtp when smtp.Message.Contains("STARTTLS", StringComparison.OrdinalIgnoreCase) =>
                "SMTP requires STARTTLS. Try port 587 with SMTP SSL enabled.",
            _ => ex.Message
        };

    #endregion
}
