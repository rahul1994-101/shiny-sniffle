namespace Application.Features.UserSettings;

public class GeneralSettingsDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public static GeneralSettingsDto FromEntity(User user) => new()
    {
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName
    };

    public T AsResponse<T>() where T : GeneralSettingsDto, new() => new()
    {
        Email = Email,
        FirstName = FirstName,
        LastName = LastName
    };
}

public class EmailSettingsDto
{
    public EmailProvider Provider { get; set; } = EmailProvider.Custom;

    public string ProviderSlug { get; set; } = "custom";

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

    public EmailSettingsDto CloneForBaseline()
    {
        var clone = CloneShallow();
        clone.Password = string.Empty;
        return clone;
    }

    public bool ContentEquals(EmailSettingsDto other) =>
        string.Equals(ProviderSlug, other.ProviderSlug, StringComparison.OrdinalIgnoreCase)
        && string.Equals(EmailAddress, other.EmailAddress, StringComparison.Ordinal)
        && string.Equals(Username, other.Username, StringComparison.Ordinal)
        && HasStoredPassword == other.HasStoredPassword;

    private EmailSettingsDto CloneShallow() => new()
    {
        Provider = Provider,
        ProviderSlug = ProviderSlug,
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

    public T AsResponse<T>() where T : EmailSettingsDto, new() => new()
    {
        Provider = Provider,
        ProviderSlug = ProviderSlug,
        EmailAddress = EmailAddress,
        ImapHost = ImapHost,
        ImapPort = ImapPort,
        ImapUseSsl = ImapUseSsl,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUseSsl = SmtpUseSsl,
        Username = Username,
        Password = Password,
        HasStoredPassword = HasStoredPassword
    };
}
