using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Mailbox;

internal static class MailboxConnectionHelpers
{
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

    internal static ImapClient CreateImapClient() =>
        new()
        {
            Timeout = ConnectTimeoutMs,
            // Intentional v1 tradeoff: many hosted mail servers use misconfigured or private CAs;
            // disabling revocation checks avoids false negatives on connect. Revisit if exposing a strict-TLS option.
            CheckCertificateRevocation = false
        };

    internal static SmtpClient CreateSmtpClient() =>
        new()
        {
            Timeout = ConnectTimeoutMs,
            CheckCertificateRevocation = false
        };

    internal static Task ConnectImapAsync(ImapClient client, EmailSettings config, CancellationToken cancellationToken)
    {
        var secure = GetImapSecureSocketOptions(config.ImapPort, config.ImapUseSsl);
        return client.ConnectAsync(config.ImapHost, config.ImapPort, secure, cancellationToken);
    }

    internal static Task ConnectSmtpAsync(SmtpClient client, EmailSettings config, CancellationToken cancellationToken)
    {
        var secure = GetSmtpSecureSocketOptions(config.SmtpPort, config.SmtpUseSsl);
        return client.ConnectAsync(config.SmtpHost, config.SmtpPort, secure, cancellationToken);
    }

    internal static async Task<ConnectionProbeResult> ProbeConnectionsAsync(
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
}
