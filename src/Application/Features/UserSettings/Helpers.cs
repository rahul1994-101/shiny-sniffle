using Application.Features.EmailProviders;
using Application.Utilities.Extensions;

namespace Application.Features.UserSettings;

internal enum EmailSettingsBuildMode
{
    Save,
    Draft
}

internal readonly record struct EmailProviderEndpoints(
    string ImapHost,
    int ImapPort,
    bool ImapUseSsl,
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseSsl);

public static class EmailProviderPresets
{
    private static readonly EmailProviderEndpoints Gmail = new(
        ImapHost: "imap.gmail.com",
        ImapPort: 993,
        ImapUseSsl: true,
        SmtpHost: "smtp.gmail.com",
        SmtpPort: 587,
        SmtpUseSsl: true);

    internal static EmailProviderEndpoints? GetEndpoints(EmailProvider provider) =>
        provider switch
        {
            EmailProvider.Gmail => Gmail,
            EmailProvider.Custom => null,
            _ => null
        };

    internal static void Apply(EmailSettings settings)
    {
        var endpoints = GetEndpoints(settings.Provider);
        if (endpoints is null)
        {
            return;
        }

        settings.ImapHost = endpoints.Value.ImapHost;
        settings.ImapPort = endpoints.Value.ImapPort;
        settings.ImapUseSsl = endpoints.Value.ImapUseSsl;
        settings.SmtpHost = endpoints.Value.SmtpHost;
        settings.SmtpPort = endpoints.Value.SmtpPort;
        settings.SmtpUseSsl = endpoints.Value.SmtpUseSsl;
    }

    public static void Apply(EmailSettingsDto response)
    {
        var endpoints = GetEndpoints(response.Provider);
        if (endpoints is null)
        {
            return;
        }

        response.ImapHost = endpoints.Value.ImapHost;
        response.ImapPort = endpoints.Value.ImapPort;
        response.ImapUseSsl = endpoints.Value.ImapUseSsl;
        response.SmtpHost = endpoints.Value.SmtpHost;
        response.SmtpPort = endpoints.Value.SmtpPort;
        response.SmtpUseSsl = endpoints.Value.SmtpUseSsl;
    }

    public static void ClearEndpoints(EmailSettingsDto response)
    {
        response.ImapHost = string.Empty;
        response.SmtpHost = string.Empty;
        response.ImapPort = 993;
        response.SmtpPort = 587;
        response.ImapUseSsl = true;
        response.SmtpUseSsl = true;
    }

    internal static bool Matches(EmailSettings settings, EmailProvider provider)
    {
        var endpoints = GetEndpoints(provider);
        if (endpoints is null)
        {
            return false;
        }

        return HostEquals(settings.ImapHost, endpoints.Value.ImapHost) &&
               settings.ImapPort == endpoints.Value.ImapPort &&
               settings.ImapUseSsl == endpoints.Value.ImapUseSsl &&
               HostEquals(settings.SmtpHost, endpoints.Value.SmtpHost) &&
               settings.SmtpPort == endpoints.Value.SmtpPort &&
               settings.SmtpUseSsl == endpoints.Value.SmtpUseSsl;
    }

    private static bool HostEquals(string left, string right) =>
        string.Equals(left.Trim(), right, StringComparison.OrdinalIgnoreCase);
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
            ProviderSlug = ResolveProviderSlug(stored, provider),
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

            if (string.IsNullOrWhiteSpace(response.ImapHost) || string.IsNullOrWhiteSpace(response.SmtpHost))
            {
                return "Mail provider server settings are missing. Configure them under Settings → Email → Providers.";
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
            ProviderSlug = settings.ProviderSlug,
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
        return new EmailSettings
        {
            Provider = response.Provider,
            ProviderSlug = EmailProviderCatalog.NormalizeSlug(response.ProviderSlug),
            EmailAddress = response.EmailAddress.Trim(),
            Username = response.Username.Trim(),
            Password = password,
            ImapHost = response.ImapHost.Trim(),
            ImapPort = response.ImapPort,
            ImapUseSsl = response.ImapUseSsl,
            SmtpHost = response.SmtpHost.Trim(),
            SmtpPort = response.SmtpPort,
            SmtpUseSsl = response.SmtpUseSsl
        };
    }

    private static string ResolveProviderSlug(EmailSettings stored, EmailProvider provider)
    {
        if (!string.IsNullOrWhiteSpace(stored.ProviderSlug))
        {
            return EmailProviderCatalog.NormalizeSlug(stored.ProviderSlug);
        }

        return provider == EmailProvider.Gmail ? "gmail" : "custom";
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

        return true;
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
