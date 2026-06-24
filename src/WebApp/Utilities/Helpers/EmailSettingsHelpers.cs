using WebApp.Models;
using WebApp.Utilities.Extensions;

namespace WebApp.Utilities.Helpers;

internal static class EmailSettingsHelpers
{
    internal static EmailSettings? FromJson(string? json) =>
        JsonColumnHelpers.Deserialize<EmailSettings>(json);

    internal static string? ToJson(EmailSettings? settings) =>
        JsonColumnHelpers.Serialize(settings);

    internal static void ApplyProviderEndpoints(EmailSettingsDto dto)
    {
        EmailProviderPresets.ApplyToDto(dto);
    }

    internal static void ClearProviderEndpoints(EmailSettingsDto dto)
    {
        EmailProviderPresets.ClearDtoEndpoints(dto);
    }

    internal static EmailSettingsDto MapToDto(EmailSettings? stored)
    {
        if (stored is null)
        {
            return new EmailSettingsDto();
        }

        var provider = ResolveProvider(stored);

        return new EmailSettingsDto
        {
            Provider = provider,
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

    internal static string? TryBuildForSave(EmailSettingsDto email, EmailSettings? existing, out EmailSettings? settings)
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

        if (string.IsNullOrWhiteSpace(email.Username))
        {
            return "Mailbox username is required.";
        }

        if (string.IsNullOrWhiteSpace(email.Password) && !hasStoredPassword)
        {
            return "Mailbox password is required.";
        }

        if (email.Provider == EmailProvider.Custom)
        {
            if (string.IsNullOrWhiteSpace(email.ImapHost))
            {
                return "IMAP host is required for mailbox settings.";
            }

            if (string.IsNullOrWhiteSpace(email.SmtpHost))
            {
                return "SMTP host is required for mailbox settings.";
            }
        }

        settings = new EmailSettings
        {
            Provider = email.Provider,
            EmailAddress = email.EmailAddress.Trim(),
            Username = email.Username.Trim(),
            Password = ResolvePasswordForSave(email.Password, existing?.Password)
        };

        if (email.Provider == EmailProvider.Custom)
        {
            settings.ImapHost = email.ImapHost.Trim();
            settings.ImapPort = email.ImapPort;
            settings.ImapUseSsl = email.ImapUseSsl;
            settings.SmtpHost = email.SmtpHost.Trim();
            settings.SmtpPort = email.SmtpPort;
            settings.SmtpUseSsl = email.SmtpUseSsl;
        }
        else
        {
            EmailProviderPresets.ApplyToEntity(settings);
        }

        return null;
    }

    internal static MailboxConnectionOptions? ResolveConnectionOptions(EmailSettings? stored, EmailSettingsDto? draft = null)
    {
        if (draft is null)
        {
            return stored is null ? null : ToConnectionOptions(stored);
        }

        var merged = MergeDraft(stored, draft);
        return merged is null ? null : ToConnectionOptions(merged);
    }

    private static EmailProvider ResolveProvider(EmailSettings stored)
    {
        if (stored.Provider != EmailProvider.Custom && Enum.IsDefined(stored.Provider))
        {
            return stored.Provider;
        }

        if (EmailProviderPresets.Matches(stored, EmailProvider.Gmail))
        {
            return EmailProvider.Gmail;
        }

        return EmailProvider.Custom;
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

        var provider = ResolveProvider(stored);

        return new MailboxConnectionOptions
        {
            Provider = EmailProviderPresets.ToConnectionName(provider),
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

    private static bool IsEmpty(EmailSettingsDto email)
    {
        if (!string.IsNullOrWhiteSpace(email.EmailAddress) ||
            !string.IsNullOrWhiteSpace(email.Username) ||
            !string.IsNullOrWhiteSpace(email.Password))
        {
            return false;
        }

        if (email.Provider != EmailProvider.Custom)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(email.ImapHost) &&
               string.IsNullOrWhiteSpace(email.SmtpHost);
    }

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

        var settings = new EmailSettings
        {
            Provider = draft.Provider,
            EmailAddress = draft.EmailAddress.Trim(),
            Username = draft.Username.Trim(),
            Password = password
        };

        if (draft.Provider == EmailProvider.Custom)
        {
            settings.ImapHost = draft.ImapHost.Trim();
            settings.ImapPort = draft.ImapPort;
            settings.ImapUseSsl = draft.ImapUseSsl;
            settings.SmtpHost = draft.SmtpHost.Trim();
            settings.SmtpPort = draft.SmtpPort;
            settings.SmtpUseSsl = draft.SmtpUseSsl;
        }
        else
        {
            EmailProviderPresets.ApplyToEntity(settings);
        }

        return settings;
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
