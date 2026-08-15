using Application.Features.Workspace.EmailAccounts;

namespace Application.Features.Dbo.EmailProviders;

internal static partial class EmailProviderMapping
{
    private const int NameMaxLength = 100;
    private const int HostMaxLength = 255;
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

        return null;
    }
}

public static class EmailProviderCatalog
{
    /// <summary>
    /// Known IDs for seeded system templates (see dbo/Tables/EmailProvider.sql).
    /// Convention for cross-environment debug — app logic must not require these at runtime.
    /// </summary>
    public static class SystemIds
    {
        public static readonly Guid Gmail = Guid.Parse("E1000001-0000-4000-8000-000000000001");

        public static readonly Guid Outlook = Guid.Parse("E1000002-0000-4000-8000-000000000002");

        public static readonly Guid Yahoo = Guid.Parse("E1000003-0000-4000-8000-000000000003");

        public static readonly Guid ICloud = Guid.Parse("E1000004-0000-4000-8000-000000000004");

        public static readonly Guid Zoho = Guid.Parse("E1000005-0000-4000-8000-000000000005");

        public static readonly Guid Fastmail = Guid.Parse("E1000006-0000-4000-8000-000000000006");
    }

    public static EmailProviderDto? FindById(IReadOnlyList<EmailProviderDto> catalog, Guid providerId)
    {
        if (providerId == Guid.Empty)
        {
            return null;
        }

        return catalog.FirstOrDefault(p => p.Id == providerId);
    }

    public static void ApplyTo(EmailSettingsDto dto, EmailProviderDto catalog)
    {
        dto.EmailProviderId = catalog.Id;
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
