using Application.Features.Workspace.EmailAccounts;

namespace Application.Features.Dbo.EmailProviders;

internal static partial class EmailProviderMapping
{
    private const int NameMaxLength = 100;
    private const int HostMaxLength = 255;
    private const int SetupHelpUrlMaxLength = 500;
    private const int PortMin = 1;
    private const int PortMax = 65535;

    internal static string? ValidatePort(int port, string label)
    {
        if (port is < PortMin or > PortMax)
        {
            return $"{label} port must be between {PortMin} and {PortMax}.";
        }

        return null;
    }

    internal static string? ValidateSave(SaveEmailProviderDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return "Name is required.";
        }

        if (name.Length > NameMaxLength)
        {
            return $"Name must be {NameMaxLength} characters or fewer.";
        }

        var slugError = CatalogFieldRules.ValidateSlug(dto.Slug, required: false);
        if (slugError is not null)
        {
            return slugError;
        }

        if (string.IsNullOrWhiteSpace(dto.ImapHost))
        {
            return "IMAP host is required.";
        }

        if (dto.ImapHost.Trim().Length > HostMaxLength)
        {
            return $"IMAP host must be {HostMaxLength} characters or fewer.";
        }

        if (string.IsNullOrWhiteSpace(dto.SmtpHost))
        {
            return "SMTP host is required.";
        }

        if (dto.SmtpHost.Trim().Length > HostMaxLength)
        {
            return $"SMTP host must be {HostMaxLength} characters or fewer.";
        }

        var imapPortError = ValidatePort(dto.ImapPort, "IMAP");
        if (imapPortError is not null)
        {
            return imapPortError;
        }

        var smtpPortError = ValidatePort(dto.SmtpPort, "SMTP");
        if (smtpPortError is not null)
        {
            return smtpPortError;
        }

        if (dto.SetupHelpUrl is not null && dto.SetupHelpUrl.Trim().Length > SetupHelpUrlMaxLength)
        {
            return $"Setup help URL must be {SetupHelpUrlMaxLength} characters or fewer.";
        }

        return null;
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
    public static string NormalizeSlug(string? slug) => CatalogFieldRules.NormalizeSlug(slug);

    public static EmailProviderDto? FindBySlug(IReadOnlyList<EmailProviderDto> catalog, string? slug)
    {
        var normalized = NormalizeSlug(slug);
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        return catalog.FirstOrDefault(p =>
            string.Equals(p.Slug, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static void ApplyTo(EmailSettingsDto dto, EmailProviderDto catalog)
    {
        dto.ProviderSlug = catalog.Slug;
        dto.ImapHost = catalog.ImapHost;
        dto.ImapPort = catalog.ImapPort;
        dto.ImapUseSsl = catalog.ImapUseSsl;
        dto.SmtpHost = catalog.SmtpHost;
        dto.SmtpPort = catalog.SmtpPort;
        dto.SmtpUseSsl = catalog.SmtpUseSsl;
    }

    public static string? ValidateCatalogEndpoints(EmailProviderDto catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.ImapHost) || string.IsNullOrWhiteSpace(catalog.SmtpHost))
        {
            return "This provider has no IMAP/SMTP hosts configured. Update it under Settings → Email providers.";
        }

        var imapPortError = EmailProviderMapping.ValidatePort(catalog.ImapPort, "IMAP");
        if (imapPortError is not null)
        {
            return imapPortError;
        }

        var smtpPortError = EmailProviderMapping.ValidatePort(catalog.SmtpPort, "SMTP");
        if (smtpPortError is not null)
        {
            return smtpPortError;
        }

        return null;
    }
}
