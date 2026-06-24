using WebApp.Models;
using WebApp.Utilities.Extensions;

namespace WebApp.Utilities.Helpers;

internal enum EmailSettingsBuildMode
{
    Save,
    Draft
}

internal static class EmailSettingsHelpers
{
    internal static EmailSettings? FromJson(string? json) =>
        JsonColumnHelpers.Deserialize<EmailSettings>(json);

    internal static string? ToJson(EmailSettings? settings) =>
        JsonColumnHelpers.Serialize(settings);

    internal static EmailSettingsDto ToDto(EmailSettings? stored)
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

    internal static string? TryBuildFromDto(EmailSettingsDto dto, EmailSettings? existing, EmailSettingsBuildMode mode, out EmailSettings? settings)
    {
        settings = null;

        if (mode == EmailSettingsBuildMode.Save && IsEmpty(dto))
        {
            return null;
        }

        var password = ResolvePassword(dto.Password, existing?.Password);

        if (mode == EmailSettingsBuildMode.Save)
        {
            if (string.IsNullOrWhiteSpace(dto.EmailAddress))
            {
                return "Email address is required for mailbox settings.";
            }

            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return "Mailbox username is required.";
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return "Mailbox password is required.";
            }

            if (dto.Provider == EmailProvider.Custom)
            {
                if (string.IsNullOrWhiteSpace(dto.ImapHost))
                {
                    return "IMAP host is required for mailbox settings.";
                }

                if (string.IsNullOrWhiteSpace(dto.SmtpHost))
                {
                    return "SMTP host is required for mailbox settings.";
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        settings = CreateEntity(dto, password);
        return null;
    }

    internal static EmailSettings? ResolveForMail(EmailSettings? stored, EmailSettingsDto? draft)
    {
        if (draft is null)
        {
            return stored;
        }

        _ = TryBuildFromDto(draft, stored, EmailSettingsBuildMode.Draft, out var merged);
        return merged;
    }

    private static EmailSettings CreateEntity(EmailSettingsDto dto, string password)
    {
        var settings = new EmailSettings
        {
            Provider = dto.Provider,
            EmailAddress = dto.EmailAddress.Trim(),
            Username = dto.Username.Trim(),
            Password = password
        };

        if (dto.Provider == EmailProvider.Custom)
        {
            settings.ImapHost = dto.ImapHost.Trim();
            settings.ImapPort = dto.ImapPort;
            settings.ImapUseSsl = dto.ImapUseSsl;
            settings.SmtpHost = dto.SmtpHost.Trim();
            settings.SmtpPort = dto.SmtpPort;
            settings.SmtpUseSsl = dto.SmtpUseSsl;
        }
        else
        {
            EmailProviderPresets.Apply(settings);
        }

        return settings;
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

    private static string ResolvePassword(string plainPassword, string? existingEncryptedPassword)
    {
        if (!string.IsNullOrWhiteSpace(plainPassword))
        {
            return plainPassword.Trim().Encrypt();
        }

        return existingEncryptedPassword ?? string.Empty;
    }
}
