using Application.Features.dbo.EmailProviders;
using Application.Features.Shared;
using Application.Utilities.Extensions;
using Infrastructure.Persistence.dbo;
using Infrastructure.Persistence.workspace;

namespace Application.Features.workspace.EmailAccounts;

internal static class EmailAccountMapping
{
    internal static EmailAccountSummaryDto ToSummary(EmailAccount account, EmailProvider provider) => new()
    {
        Id = account.Id,
        Alias = account.Alias,
        ProviderName = provider.Name,
        ProviderSlug = provider.Slug,
        EmailAddress = account.EmailAddress,
        IsDefault = account.IsDefault,
        SortOrder = account.SortOrder
    };

    internal static EmailAccountDto ToDto(EmailAccount account, EmailProvider provider)
    {
        var settings = ToEmailSettings(account, provider);
        return new EmailAccountDto
        {
            Id = account.Id,
            Alias = account.Alias,
            IsDefault = account.IsDefault,
            Provider = settings.Provider,
            ProviderSlug = settings.ProviderSlug,
            ProviderName = provider.Name,
            EmailAddress = settings.EmailAddress,
            ImapHost = settings.ImapHost,
            ImapPort = settings.ImapPort,
            ImapUseSsl = settings.ImapUseSsl,
            SmtpHost = settings.SmtpHost,
            SmtpPort = settings.SmtpPort,
            SmtpUseSsl = settings.SmtpUseSsl,
            Username = settings.Username,
            HasStoredPassword = !string.IsNullOrWhiteSpace(settings.Password),
            Context = account.Context
        };
    }

    internal static EmailSettings ToEmailSettings(EmailAccount account, EmailProvider provider)
    {
        var slug = EmailProviderCatalog.NormalizeSlug(provider.Slug);
        return new EmailSettings
        {
            Provider = EmailProviderCatalog.ToLegacyProvider(slug),
            ProviderSlug = slug,
            EmailAddress = account.EmailAddress,
            Username = account.Username,
            Password = account.Password,
            ImapHost = provider.ImapHost,
            ImapPort = provider.ImapPort,
            ImapUseSsl = provider.ImapUseSsl,
            SmtpHost = provider.SmtpHost,
            SmtpPort = provider.SmtpPort,
            SmtpUseSsl = provider.SmtpUseSsl
        };
    }

    internal static EmailSettingsDto ToSettingsDto(EmailAccountDto account) => new()
    {
        Provider = account.Provider,
        ProviderSlug = account.ProviderSlug,
        EmailAddress = account.EmailAddress,
        ImapHost = account.ImapHost,
        ImapPort = account.ImapPort,
        ImapUseSsl = account.ImapUseSsl,
        SmtpHost = account.SmtpHost,
        SmtpPort = account.SmtpPort,
        SmtpUseSsl = account.SmtpUseSsl,
        Username = account.Username,
        HasStoredPassword = account.HasStoredPassword
    };

    internal static EmailSettingsDto ToSettingsDto(SaveEmailAccountDto save, EmailSettingsDto catalogApplied) => new()
    {
        Provider = catalogApplied.Provider,
        ProviderSlug = catalogApplied.ProviderSlug,
        EmailAddress = save.EmailAddress,
        ImapHost = catalogApplied.ImapHost,
        ImapPort = catalogApplied.ImapPort,
        ImapUseSsl = catalogApplied.ImapUseSsl,
        SmtpHost = catalogApplied.SmtpHost,
        SmtpPort = catalogApplied.SmtpPort,
        SmtpUseSsl = catalogApplied.SmtpUseSsl,
        Username = save.Username,
        Password = save.Password,
        HasStoredPassword = false
    };

    internal static string? NormalizeAlias(string? value) => EntityAliasRules.NormalizeOptional(value);

    internal static string BuildAliasStem(string emailAddress) =>
        EntityAliasRules.StemFromEmailAddress(emailAddress);

    internal static string AliasWithNumericSuffix(string stem, int index) =>
        EntityAliasRules.WithNumericSuffix(stem, index, "mailbox");

    internal static string? ValidateSave(SaveEmailAccountDto dto)
    {
        var alias = NormalizeAlias(dto.Alias);
        if (alias is not null && alias.Length > EntityAliasRules.MaxLength)
        {
            return "Alias must be 64 characters or fewer.";
        }

        if (dto.Context is not null && dto.Context.Trim().Length > 2000)
        {
            return "Context must be 2000 characters or fewer.";
        }

        return null;
    }
}

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

    internal static EmailProviderEndpoints? GetEndpoints(EmailProviderPreset provider) =>
        provider switch
        {
            EmailProviderPreset.Gmail => Gmail,
            EmailProviderPreset.Custom => null,
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

    internal static bool Matches(EmailSettings settings, EmailProviderPreset provider)
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

    private static string ResolveProviderSlug(EmailSettings stored, EmailProviderPreset provider)
    {
        if (!string.IsNullOrWhiteSpace(stored.ProviderSlug))
        {
            return EmailProviderCatalog.NormalizeSlug(stored.ProviderSlug);
        }

        return provider == EmailProviderPreset.Gmail ? "gmail" : "custom";
    }

    private static EmailProviderPreset ResolveProvider(EmailSettings stored)
    {
        if (stored.Provider != EmailProviderPreset.Custom && Enum.IsDefined(stored.Provider))
        {
            return stored.Provider;
        }

        if (EmailProviderPresets.Matches(stored, EmailProviderPreset.Gmail))
        {
            return EmailProviderPreset.Gmail;
        }

        return EmailProviderPreset.Custom;
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

internal static class EmailSettingsCatalog
{
    internal static async Task<(IReadOnlyList<EmailProviderDto> Catalog, string? Error)> LoadCatalogAsync(
        EmailProviderRepository emailProviderRepo,
        CancellationToken cancellationToken)
    {
        var catalog = await emailProviderRepo.ListAsync(cancellationToken);
        if (catalog.Count == 0)
        {
            return (catalog, "No mail providers are configured. Add templates under Settings → Email → Providers.");
        }

        return (catalog, null);
    }

    internal static string? TryApplyCatalog(EmailSettingsDto dto, IReadOnlyList<EmailProviderDto> catalog)
    {
        var row = EmailProviderCatalog.FindBySlug(catalog, dto.ProviderSlug);
        if (row is null)
        {
            return "Select a valid mail provider.";
        }

        var endpointError = EmailProviderCatalog.ValidateCatalogEndpoints(row);
        if (endpointError is not null)
        {
            return endpointError;
        }

        EmailProviderCatalog.ApplyTo(dto, row);
        return null;
    }
}
