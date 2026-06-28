namespace WebApp.Utilities.Helpers;

internal readonly record struct EmailProviderEndpoints(
    string ImapHost,
    int ImapPort,
    bool ImapUseSsl,
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseSsl);

internal static class EmailProviderPresets
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

    internal static void Apply(EmailSettingsDto dto)
    {
        var endpoints = GetEndpoints(dto.Provider);
        if (endpoints is null)
        {
            return;
        }

        dto.ImapHost = endpoints.Value.ImapHost;
        dto.ImapPort = endpoints.Value.ImapPort;
        dto.ImapUseSsl = endpoints.Value.ImapUseSsl;
        dto.SmtpHost = endpoints.Value.SmtpHost;
        dto.SmtpPort = endpoints.Value.SmtpPort;
        dto.SmtpUseSsl = endpoints.Value.SmtpUseSsl;
    }

    internal static void ClearDtoEndpoints(EmailSettingsDto dto)
    {
        dto.ImapHost = string.Empty;
        dto.SmtpHost = string.Empty;
        dto.ImapPort = 993;
        dto.SmtpPort = 587;
        dto.ImapUseSsl = true;
        dto.SmtpUseSsl = true;
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
