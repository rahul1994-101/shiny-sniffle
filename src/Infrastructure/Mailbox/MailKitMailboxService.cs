using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MimeKit;
using System.Net.Mail;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Mailbox;

public sealed class MailKitMailboxService : IMailboxService
{
    public async Task<MailboxStatusResult> GetStatusAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        var probe = await MailboxConnectionHelpers.ProbeConnectionsAsync(config, cancellationToken);

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
            Message = MailboxConnectionHelpers.FormatConnectionProbeMessage(probe)
        };
    }

    public async Task<MailboxTestResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        var probe = await MailboxConnectionHelpers.ProbeConnectionsAsync(config, cancellationToken);

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
            Message = MailboxConnectionHelpers.FormatConnectionProbeMessage(probe)
        };
    }

    public async Task<InboxListResult> ListMessagesAsync(EmailSettings config, InboxQuery query, CancellationToken cancellationToken = default)
    {
        using var imap = MailboxConnectionHelpers.CreateImapClient();
        await MailboxConnectionHelpers.ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, query.Folder, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var result = await MailboxSummaryHelpers.ListAsync(folder, query, cancellationToken);

        await imap.DisconnectAsync(true, cancellationToken);
        return result;
    }

    public async Task<InboxMessageDetail?> GetMessageAsync(EmailSettings config, uint uid, string? folder = null, CancellationToken cancellationToken = default)
    {
        using var imap = MailboxConnectionHelpers.CreateImapClient();
        await MailboxConnectionHelpers.ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var mailFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, folder, cancellationToken);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var detail = await MailboxMessageHelpers.GetDetailAsync(mailFolder, uid, cancellationToken);

        await imap.DisconnectAsync(true, cancellationToken);
        return detail;
    }

    public async Task<IReadOnlyList<InboxMessageDetail>> GetMessagesAsync(
        EmailSettings config,
        IReadOnlyList<MessageRef> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return [];
        }

        if (messages.Count > MailboxReadLimits.MaxBatchGetCount)
        {
            throw new ArgumentException(
                $"At most {MailboxReadLimits.MaxBatchGetCount} messages can be read per call.",
                nameof(messages));
        }

        using var imap = MailboxConnectionHelpers.CreateImapClient();
        await MailboxConnectionHelpers.ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var details = await MailboxMessageHelpers.GetManyAsync(imap, messages, cancellationToken);

        await imap.DisconnectAsync(true, cancellationToken);
        return details;
    }

    public async Task<IReadOnlyList<MailboxFolderInfo>> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        using var imap = MailboxConnectionHelpers.CreateImapClient();
        await MailboxConnectionHelpers.ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var folders = new List<MailboxFolderInfo>();

        if (imap.Inbox is not null)
        {
            folders.Add(MailboxFolderResolverHelpers.MapFolder(imap.Inbox));
        }

        foreach (var ns in imap.PersonalNamespaces)
        {
            var root = imap.GetFolder(ns);
            await MailboxFolderResolverHelpers.CollectFoldersAsync(root, folders, cancellationToken);
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

        using var smtp = MailboxConnectionHelpers.CreateSmtpClient();
        await MailboxConnectionHelpers.ConnectSmtpAsync(smtp, config, cancellationToken);
        await smtp.AuthenticateAsync(config.Username, config.Password, cancellationToken);
        await smtp.SendAsync(message, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);

        return new SendMailResult
        {
            Success = true,
            Message = $"Email sent to {mail.To}."
        };
    }

    public async Task<MailboxCommandResult> DeleteMessagesAsync(
        EmailSettings config,
        IReadOnlyList<MessageRef> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return new MailboxCommandResult
            {
                Success = false,
                Message = "No messages were specified."
            };
        }

        using var imap = MailboxConnectionHelpers.CreateImapClient();
        await MailboxConnectionHelpers.ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var result = await MailboxCommandsHelpers.DeleteAsync(imap, messages, cancellationToken);

        await imap.DisconnectAsync(true, cancellationToken);
        return result;
    }

    public async Task<MailboxCommandResult> MoveMessagesAsync(
        EmailSettings config,
        IReadOnlyList<MessageRef> messages,
        string destinationFolder,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return new MailboxCommandResult
            {
                Success = false,
                Message = "No messages were specified."
            };
        }

        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            return new MailboxCommandResult
            {
                Success = false,
                Message = "Destination folder is required."
            };
        }

        using var imap = MailboxConnectionHelpers.CreateImapClient();
        await MailboxConnectionHelpers.ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var result = await MailboxCommandsHelpers.MoveAsync(imap, messages, destinationFolder.Trim(), cancellationToken);

        await imap.DisconnectAsync(true, cancellationToken);
        return result;
    }

    public async Task<MailboxCommandResult> SetMessageFlagsAsync(
        EmailSettings config,
        IReadOnlyList<MessageRef> messages,
        MessageFlagAction flag,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return new MailboxCommandResult
            {
                Success = false,
                Message = "No messages were specified."
            };
        }

        using var imap = MailboxConnectionHelpers.CreateImapClient();
        await MailboxConnectionHelpers.ConnectImapAsync(imap, config, cancellationToken);
        await imap.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var result = await MailboxCommandsHelpers.SetFlagsAsync(imap, messages, flag, cancellationToken);

        await imap.DisconnectAsync(true, cancellationToken);
        return result;
    }
}
