namespace WebApp.Features.Settings;

public sealed class EmailSettingsResponse
{
    public EmailProvider Provider { get; set; } = EmailProvider.Custom;
    public string EmailAddress { get; set; } = string.Empty;
    public string ImapHost { get; set; } = string.Empty;
    public int ImapPort { get; set; } = 993;
    public bool ImapUseSsl { get; set; } = true;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool HasStoredPassword { get; set; }

    internal EmailSettingsResponse CloneForBaseline()
    {
        var clone = CloneShallow();
        clone.Password = string.Empty;
        return clone;
    }

    internal bool ContentEquals(EmailSettingsResponse other) =>
        Provider == other.Provider
        && string.Equals(EmailAddress, other.EmailAddress, StringComparison.Ordinal)
        && string.Equals(Username, other.Username, StringComparison.Ordinal)
        && string.Equals(ImapHost, other.ImapHost, StringComparison.Ordinal)
        && ImapPort == other.ImapPort
        && ImapUseSsl == other.ImapUseSsl
        && string.Equals(SmtpHost, other.SmtpHost, StringComparison.Ordinal)
        && SmtpPort == other.SmtpPort
        && SmtpUseSsl == other.SmtpUseSsl
        && HasStoredPassword == other.HasStoredPassword;

    private EmailSettingsResponse CloneShallow() => new()
    {
        Provider = Provider,
        EmailAddress = EmailAddress,
        Username = Username,
        ImapHost = ImapHost,
        ImapPort = ImapPort,
        ImapUseSsl = ImapUseSsl,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUseSsl = SmtpUseSsl,
        HasStoredPassword = HasStoredPassword,
        Password = Password
    };
}
