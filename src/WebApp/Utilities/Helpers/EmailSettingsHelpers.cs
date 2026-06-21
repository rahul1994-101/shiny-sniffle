using WebApp.Models;
using WebApp.Utilities.Extensions;

namespace WebApp.Utilities.Helpers;

internal static class EmailSettingsHelpers
{
    internal static EmailSettings? FromJson(string? json) =>
        JsonColumnHelpers.Deserialize<EmailSettings>(json);

    internal static string? ToJson(EmailSettings? settings) =>
        JsonColumnHelpers.Serialize(settings);

    internal static EmailSettingsDto MapToDto(EmailSettings? stored)
    {
        if (stored is null)
        {
            return new EmailSettingsDto();
        }

        return new EmailSettingsDto
        {
            EmailAddress = stored.EmailAddress,
            ImapHost = stored.ImapHost,
            ImapPort = stored.ImapPort,
            ImapUseSsl = stored.ImapUseSsl,
            SmtpHost = stored.SmtpHost,
            SmtpPort = stored.SmtpPort,
            SmtpUseSsl = stored.SmtpUseSsl,
            Username = stored.Username,
            Password = string.Empty,
            HasStoredPassword = !string.IsNullOrWhiteSpace(stored.Password)
        };
    }

    internal static string? TryBuildForSave(
        EmailSettingsDto email,
        EmailSettings? existing,
        out EmailSettings? settings)
    {
        settings = null;

        if (IsEmpty(email))
        {
            return null;
        }

        var hasStoredPassword = !string.IsNullOrWhiteSpace(existing?.Password);

        if (string.IsNullOrWhiteSpace(email.EmailAddress))
        {
            return "Email address is required for mailbox settings.";
        }

        if (string.IsNullOrWhiteSpace(email.ImapHost))
        {
            return "IMAP host is required for mailbox settings.";
        }

        if (string.IsNullOrWhiteSpace(email.SmtpHost))
        {
            return "SMTP host is required for mailbox settings.";
        }

        if (string.IsNullOrWhiteSpace(email.Username))
        {
            return "Mailbox username is required.";
        }

        if (string.IsNullOrWhiteSpace(email.Password) && !hasStoredPassword)
        {
            return "Mailbox password is required.";
        }

        settings = new EmailSettings
        {
            EmailAddress = email.EmailAddress.Trim(),
            ImapHost = email.ImapHost.Trim(),
            ImapPort = email.ImapPort,
            ImapUseSsl = email.ImapUseSsl,
            SmtpHost = email.SmtpHost.Trim(),
            SmtpPort = email.SmtpPort,
            SmtpUseSsl = email.SmtpUseSsl,
            Username = email.Username.Trim(),
            Password = ResolvePasswordForSave(email.Password, existing?.Password)
        };

        return null;
    }

    internal static MailboxConnectionOptions? ResolveConnectionOptions(
        EmailSettings? stored,
        EmailSettingsDto? draft = null)
    {
        if (draft is null)
        {
            return stored is null ? null : ToConnectionOptions(stored);
        }

        var merged = MergeDraft(stored, draft);
        return merged is null ? null : ToConnectionOptions(merged);
    }

    private static bool IsConfigured(EmailSettings stored) =>
        !string.IsNullOrWhiteSpace(stored.EmailAddress) &&
        !string.IsNullOrWhiteSpace(stored.ImapHost) &&
        !string.IsNullOrWhiteSpace(stored.SmtpHost) &&
        !string.IsNullOrWhiteSpace(stored.Username) &&
        !string.IsNullOrWhiteSpace(stored.Password);

    private static MailboxConnectionOptions? ToConnectionOptions(EmailSettings stored)
    {
        if (!IsConfigured(stored))
        {
            return null;
        }

        return new MailboxConnectionOptions
        {
            Provider = "generic",
            EmailAddress = stored.EmailAddress.Trim(),
            Username = stored.Username.Trim(),
            Password = stored.Password.Decrypt(),
            ImapHost = stored.ImapHost.Trim(),
            ImapPort = stored.ImapPort,
            ImapUseSsl = stored.ImapUseSsl,
            SmtpHost = stored.SmtpHost.Trim(),
            SmtpPort = stored.SmtpPort,
            SmtpUseSsl = stored.SmtpUseSsl
        };
    }

    private static bool IsEmpty(EmailSettingsDto email) =>
        string.IsNullOrWhiteSpace(email.EmailAddress) &&
        string.IsNullOrWhiteSpace(email.ImapHost) &&
        string.IsNullOrWhiteSpace(email.SmtpHost) &&
        string.IsNullOrWhiteSpace(email.Username) &&
        string.IsNullOrWhiteSpace(email.Password);

    private static string ResolvePasswordForSave(string plainPassword, string? existingEncryptedPassword)
    {
        if (!string.IsNullOrWhiteSpace(plainPassword))
        {
            return plainPassword.Trim().Encrypt();
        }

        return existingEncryptedPassword ?? string.Empty;
    }

    private static EmailSettings? MergeDraft(EmailSettings? stored, EmailSettingsDto draft)
    {
        var password = ResolveDraftPassword(draft, stored?.Password);
        if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        return new EmailSettings
        {
            EmailAddress = draft.EmailAddress.Trim(),
            ImapHost = draft.ImapHost.Trim(),
            ImapPort = draft.ImapPort,
            ImapUseSsl = draft.ImapUseSsl,
            SmtpHost = draft.SmtpHost.Trim(),
            SmtpPort = draft.SmtpPort,
            SmtpUseSsl = draft.SmtpUseSsl,
            Username = draft.Username.Trim(),
            Password = password
        };
    }

    private static string ResolveDraftPassword(EmailSettingsDto draft, string? storedEncryptedPassword)
    {
        if (!string.IsNullOrWhiteSpace(draft.Password))
        {
            return draft.Password.Trim().Encrypt();
        }

        return storedEncryptedPassword ?? string.Empty;
    }
}
