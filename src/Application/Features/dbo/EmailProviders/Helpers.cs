using Application.Features.workspace.EmailAccounts;
using Infrastructure.Mailbox;

namespace Application.Features.dbo.EmailProviders;

internal static partial class EmailProviderMapping
{
    internal static EmailProviderDto FromEntity(EmailProvider entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Slug = entity.Slug,
        ImapHost = entity.ImapHost,
        ImapPort = entity.ImapPort,
        ImapUseSsl = entity.ImapUseSsl,
        SmtpHost = entity.SmtpHost,
        SmtpPort = entity.SmtpPort,
        SmtpUseSsl = entity.SmtpUseSsl,
        SetupHelpUrl = entity.SetupHelpUrl,
        SortOrder = entity.SortOrder,
        IsSystem = entity.IsSystem
    };

    internal static string? ValidateSave(SaveEmailProviderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required.";
        }

        return CatalogFieldRules.ValidateSlug(dto.Slug, required: false);
    }

    internal static async Task<string> ResolveSlugAsync(
        Func<string, Guid?, CancellationToken, Task<bool>> isTakenAsync,
        string displayName,
        string? requestedSlug,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        await WorkspaceErAliasResolver.ResolveAsync(
            isTakenAsync,
            displayName,
            requestedSlug,
            excludeId,
            "provider",
            cancellationToken);
}

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
