using System.Globalization;
using System.Net.Mail;
using System.Text;

using WebApp.Data;
using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.Utilities.Services;

public sealed class UserMailboxService(Persistence _repo, IMailboxService _mailboxService)
{
    public async Task<string> GetMailboxStatusTextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var config = await ResolveConnectionOptionsAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return "Mailbox is not configured. Open Settings and connect IMAP/SMTP for the Email agent.";
        }

        var status = await _mailboxService.GetStatusAsync(config, cancellationToken);
        return status.Message;
    }

    public async Task<string> ListInboxMessagesTextAsync(Guid userId, string since, int limit, CancellationToken cancellationToken = default)
    {
        var config = await ResolveConnectionOptionsAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return "Mailbox is not configured. Open Settings and connect IMAP/SMTP before listing mail.";
        }

        var sinceUtc = ParseSinceUtc(since);
        if (sinceUtc is null)
        {
            return "Could not parse the date range. Use today, yesterday, last_week, or an ISO date (yyyy-MM-dd).";
        }

        try
        {
            var messages = await _mailboxService.ListInboxAsync(
                config,
                new InboxQuery { SinceUtc = sinceUtc, Limit = limit },
                cancellationToken);

            if (messages.Count == 0)
            {
                return $"No inbox messages found since {since}.";
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Inbox messages since {since} ({messages.Count}):");
            foreach (var message in messages)
            {
                builder.Append("- From: ")
                    .Append(message.From)
                    .Append(" | Subject: ")
                    .Append(message.Subject)
                    .Append(" | Date: ")
                    .Append(message.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

                if (!string.IsNullOrWhiteSpace(message.Snippet))
                {
                    builder.Append(" | ").Append(message.Snippet);
                }

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Could not list inbox messages: {ex.Message}";
        }
    }

    public async Task<string> SendEmailTextAsync(Guid userId, string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return "Recipient address is required.";
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return "Subject is required.";
        }

        if (!MailAddress.TryCreate(to, out _))
        {
            return "Recipient email address is invalid.";
        }

        var config = await ResolveConnectionOptionsAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return "Mailbox is not configured. Open Settings and connect IMAP/SMTP before sending mail.";
        }

        try
        {
            var result = await _mailboxService.SendAsync(
                config,
                new OutboundMail { To = to.Trim(), Subject = subject.Trim(), Body = body ?? string.Empty },
                cancellationToken);

            return result.Message;
        }
        catch (Exception ex)
        {
            return $"Could not send email: {ex.Message}";
        }
    }

    public async Task<MailboxTestResult> TestConnectionAsync(Guid userId, EmailSettingsDto? draft = null, CancellationToken cancellationToken = default)
    {
        var config = await ResolveConnectionOptionsAsync(userId, draft, cancellationToken);
        if (config is null)
        {
            return new MailboxTestResult
            {
                Success = false,
                Message = "Complete mailbox settings (including password) before testing the connection."
            };
        }

        return await _mailboxService.TestConnectionAsync(config, cancellationToken);
    }

    #region # Private Helpers

    private async Task<MailboxConnectionOptions?> ResolveConnectionOptionsAsync(Guid userId, EmailSettingsDto? draft = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var emailSettings = await _repo.GetUserEmailSettingsAsync(userId);
        return EmailSettingsHelpers.ResolveConnectionOptions(emailSettings, draft);
    }

    private static DateTime? ParseSinceUtc(string since)
    {
        if (string.IsNullOrWhiteSpace(since))
        {
            return DateTime.UtcNow.Date;
        }

        var value = since.Trim().ToLowerInvariant();
        var today = DateTime.UtcNow.Date;

        return value switch
        {
            "today" => today,
            "yesterday" => today.AddDays(-1),
            "last_week" or "last week" => today.AddDays(-7),
            _ => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed.ToUniversalTime().Date
                : null
        };
    }

    #endregion
}
