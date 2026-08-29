using Application.Features.Dbo.EmailProviders;
using Application.Utilities.Extensions;
using Infrastructure.Mailbox;

namespace Application.Features.Workspace.EmailAccounts;

internal static class EmailAccountMapping
{
    internal static StoredMailboxSettings ToStoredSettings(EmailAccount account, EmailProvider provider) => new()
    {
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

    internal static EmailSettingsDto ToSettingsDto(SaveEmailAccountDto save) => new()
    {
        EmailProviderId = save.EmailProviderId,
        EmailAddress = save.EmailAddress,
        Username = save.Username,
        Password = save.Password,
        HasStoredPassword = false
    };

    internal static string? NormalizeAlias(string? value) => EntityAliasRules.SlugifyOptional(value);

    internal static string? ValidateSave(SaveEmailAccountDto dto)
    {
        if (dto.EmailProviderId == Guid.Empty)
        {
            return "Select a mail provider.";
        }

        var alias = NormalizeAlias(dto.Alias);
        if (alias is not null && alias.Length > EntityAliasRules.MaxLength)
        {
            return "Alias must be 64 characters or fewer.";
        }

        if (dto.Context is not null && dto.Context.Trim().Length > CatalogFieldRules.ContextMaxLength)
        {
            return $"Context must be {CatalogFieldRules.ContextMaxLength} characters or fewer.";
        }

        return null;
    }
}

internal enum EmailSettingsBuildMode
{
    Save,
    Draft
}

internal static class EmailSettingsMapping
{
    private const int EmailAddressMaxLength = 255;
    private const int UsernameMaxLength = 255;
    private const int PasswordMaxLength = 512;

    internal static string? TryBuildStored(
        EmailSettingsDto response,
        StoredMailboxSettings? existing,
        EmailSettingsBuildMode mode,
        out StoredMailboxSettings? settings)
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
                return "Mail provider server settings are missing. Configure them under Settings → Email providers.";
            }
        }
        else if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var lengthError = ValidateFieldLengths(response);
        if (lengthError is not null)
        {
            return lengthError;
        }

        settings = CreateStored(response, password);
        return null;
    }

    private static string? ValidateFieldLengths(EmailSettingsDto response)
    {
        if (response.EmailAddress.Trim().Length > EmailAddressMaxLength)
        {
            return $"Email address must be {EmailAddressMaxLength} characters or fewer.";
        }

        if (response.Username.Trim().Length > UsernameMaxLength)
        {
            return $"Mailbox username must be {UsernameMaxLength} characters or fewer.";
        }

        if (!string.IsNullOrWhiteSpace(response.Password) && response.Password.Trim().Length > PasswordMaxLength)
        {
            return $"Mailbox password must be {PasswordMaxLength} characters or fewer.";
        }

        return null;
    }

    internal static StoredMailboxSettings? ResolveForMail(StoredMailboxSettings? stored, EmailSettingsDto? draft)
    {
        if (draft is null)
        {
            return stored;
        }

        _ = TryBuildStored(draft, stored, EmailSettingsBuildMode.Draft, out var merged);
        return merged;
    }

    internal static bool IsMailboxConfigured(StoredMailboxSettings? settings) =>
        settings is not null &&
        !string.IsNullOrWhiteSpace(settings.EmailAddress) &&
        !string.IsNullOrWhiteSpace(settings.ImapHost) &&
        !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
        !string.IsNullOrWhiteSpace(settings.Username) &&
        !string.IsNullOrWhiteSpace(settings.Password);

    internal static EmailSettings? ToMailRuntime(StoredMailboxSettings? settings)
    {
        if (settings is null || !IsMailboxConfigured(settings))
        {
            return null;
        }

        return new EmailSettings
        {
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

    private static StoredMailboxSettings CreateStored(EmailSettingsDto response, string password) => new()
    {
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
        Guid userId,
        CancellationToken cancellationToken)
    {
        var catalog = await emailProviderRepo.GetAllEmailProvidersByUserIdAsync(userId, cancellationToken);
        if (catalog.Count == 0)
        {
            return (catalog, "No mail providers are configured. Add templates under Settings → Email providers.");
        }

        return (catalog, null);
    }

    internal static string? TryApplyCatalog(EmailSettingsDto dto, IReadOnlyList<EmailProviderDto> catalog)
    {
        var row = EmailProviderCatalog.FindById(catalog, dto.EmailProviderId);
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

/// <summary>Public surface for mailbox alias preview (UI).</summary>
public static class EmailAccountAliases
{
    public static string StemFromEmailAddress(string emailAddress) =>
        EntityAliasRules.StemFromEmailAddress(emailAddress);
}
