namespace Application.Features.UserSettings;

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
