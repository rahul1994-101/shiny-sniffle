using Application.Features.EmailProviders;
using Application.Features.UserSettings;
using Infrastructure.Persistence.Entities;

namespace Application.Features.EmailAccounts;

internal static class EmailAccountMapping
{
    internal const string DefaultAlias = "Primary";

    internal static EmailAccountSummaryDto ToSummary(EmailAccount account, EmailProviderDefinition provider) => new()
    {
        Id = account.Id,
        Alias = account.Alias,
        ProviderName = provider.Name,
        ProviderSlug = provider.Slug,
        EmailAddress = account.EmailAddress,
        IsDefault = account.IsDefault,
        SortOrder = account.SortOrder
    };

    internal static EmailAccountDto ToDto(EmailAccount account, EmailProviderDefinition provider)
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
            HasStoredPassword = !string.IsNullOrWhiteSpace(settings.Password)
        };
    }

    internal static EmailSettings ToEmailSettings(EmailAccount account, EmailProviderDefinition provider)
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

    internal static string? ValidateSave(SaveEmailAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Alias))
        {
            return "Account alias is required.";
        }

        if (dto.Alias.Trim().Length > 64)
        {
            return "Alias must be 64 characters or fewer.";
        }

        return null;
    }
}
