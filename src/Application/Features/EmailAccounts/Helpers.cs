using Application.Features.EmailProviders;
using Infrastructure.Persistence.Entities;

namespace Application.Features.EmailAccounts;

internal static class EmailAccountMapping
{
    internal const string DefaultAlias = "Primary";

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
}
