using Application.Features.workspace.EmailAccounts;
using Infrastructure.Persistence.Shared;

namespace Application.Features.dbo.EmailProviders;

public static class EmailProviderCatalog
{
    public static string NormalizeSlug(string? slug) =>
        string.IsNullOrWhiteSpace(slug) ? "custom" : slug.Trim().ToLowerInvariant();

    public static EmailProviderDto? FindBySlug(IReadOnlyList<EmailProviderDto> catalog, string? slug)
    {
        var normalized = NormalizeSlug(slug);
        return catalog.FirstOrDefault(p =>
            string.Equals(p.Slug, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static void ApplyTo(EmailSettingsDto dto, EmailProviderDto catalog)
    {
        dto.ProviderSlug = catalog.Slug;
        dto.Provider = ToLegacyProvider(catalog.Slug);
        dto.ImapHost = catalog.ImapHost;
        dto.ImapPort = catalog.ImapPort;
        dto.ImapUseSsl = catalog.ImapUseSsl;
        dto.SmtpHost = catalog.SmtpHost;
        dto.SmtpPort = catalog.SmtpPort;
        dto.SmtpUseSsl = catalog.SmtpUseSsl;
    }

    public static void ApplyTo(EmailSettings settings, EmailProviderDto catalog)
    {
        settings.ProviderSlug = catalog.Slug;
        settings.Provider = ToLegacyProvider(catalog.Slug);
        settings.ImapHost = catalog.ImapHost;
        settings.ImapPort = catalog.ImapPort;
        settings.ImapUseSsl = catalog.ImapUseSsl;
        settings.SmtpHost = catalog.SmtpHost;
        settings.SmtpPort = catalog.SmtpPort;
        settings.SmtpUseSsl = catalog.SmtpUseSsl;
    }

    public static EmailProviderPreset ToLegacyProvider(string slug) =>
        string.Equals(NormalizeSlug(slug), "gmail", StringComparison.OrdinalIgnoreCase)
            ? EmailProviderPreset.Gmail
            : EmailProviderPreset.Custom;

    public static string? ValidateCatalogEndpoints(EmailProviderDto catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.ImapHost) || string.IsNullOrWhiteSpace(catalog.SmtpHost))
        {
            return "This provider has no IMAP/SMTP hosts configured. Update it under Settings → Email → Providers.";
        }

        return null;
    }
}
