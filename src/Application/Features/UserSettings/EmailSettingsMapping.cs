using Application.Utilities.Extensions;

namespace Application.Features.UserSettings;

internal enum EmailSettingsBuildMode
{
    Save,
    Draft
}

internal static class EmailSettingsMapping
{
    internal static EmailSettingsDto FromEntity(EmailSettings? stored)
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

    internal static string? TryBuildEntity(
        EmailSettingsDto response,
        EmailSettings? existing,
        EmailSettingsBuildMode mode,
        out EmailSettings? settings)
    {
        settings = null;

        if (mode == EmailSettingsBuildMode.Save && IsEmpty(response))
        {
            return null;
        }

        var password = ResolvePassword(response.Password, existing?.Password);

        if (mode == EmailSettingsBuildMode.Save)
        {
            if (string.IsNullOrWhiteSpace(response.EmailAddress))
            {
                return "Email address is required for mailbox settings.";
            }

            if (string.IsNullOrWhiteSpace(response.Username))
            {
                return "Mailbox username is required.";
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return "Mailbox password is required.";
            }

            if (response.Provider == EmailProvider.Custom)
            {
                if (string.IsNullOrWhiteSpace(response.ImapHost))
                {
                    return "IMAP host is required for mailbox settings.";
                }

                if (string.IsNullOrWhiteSpace(response.SmtpHost))
                {
                    return "SMTP host is required for mailbox settings.";
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        settings = CreateEntity(response, password);
        return null;
    }

    internal static EmailSettings? ResolveForMail(EmailSettings? stored, EmailSettingsDto? draft)
    {
        if (draft is null)
        {
            return stored;
        }

        _ = TryBuildEntity(draft, stored, EmailSettingsBuildMode.Draft, out var merged);
        return merged;
    }

    internal static bool IsMailboxConfigured(EmailSettings? settings) =>
        settings is not null &&
        !string.IsNullOrWhiteSpace(settings.EmailAddress) &&
        !string.IsNullOrWhiteSpace(settings.ImapHost) &&
        !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
        !string.IsNullOrWhiteSpace(settings.Username) &&
        !string.IsNullOrWhiteSpace(settings.Password);

    internal static EmailSettings? ToMailRuntime(EmailSettings? settings)
    {
        if (settings is null || !IsMailboxConfigured(settings))
        {
            return null;
        }

        return new EmailSettings
        {
            Provider = settings.Provider,
            EmailAddress = settings.EmailAddress.Trim(),
            Username = settings.Username.Trim(),
            Password = settings.Password.Decrypt(),
            ImapHost = settings.ImapHost.Trim(),
            ImapPort = settings.ImapPort,
            ImapUseSsl = settings.ImapUseSsl,
            SmtpHost = settings.SmtpHost.Trim(),
            SmtpPort = settings.SmtpPort,
            SmtpUseSsl = settings.SmtpUseSsl
        };
    }

    private static EmailSettings CreateEntity(EmailSettingsDto response, string password)
    {
        var settings = new EmailSettings
        {
            Provider = response.Provider,
            EmailAddress = response.EmailAddress.Trim(),
            Username = response.Username.Trim(),
            Password = password
        };

        if (response.Provider == EmailProvider.Custom)
        {
            settings.ImapHost = response.ImapHost.Trim();
            settings.ImapPort = response.ImapPort;
            settings.ImapUseSsl = response.ImapUseSsl;
            settings.SmtpHost = response.SmtpHost.Trim();
            settings.SmtpPort = response.SmtpPort;
            settings.SmtpUseSsl = response.SmtpUseSsl;
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
