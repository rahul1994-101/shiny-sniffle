namespace Application.Features.EmailAccounts;

using Application.Features.Shared;

public sealed class EmailAccountSummaryDto
{
    public Guid Id { get; init; }

    public string Alias { get; init; } = string.Empty;

    /// <summary>Typed handle for AI/tools (e.g. <c>mailbox:work</c>).</summary>
    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Mailbox, Alias);

    public string ProviderName { get; init; } = string.Empty;

    public string ProviderSlug { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public int SortOrder { get; init; }
}

public class EmailAccountDto
{
    public Guid Id { get; init; }

    public string Alias { get; init; } = string.Empty;

    /// <summary>Typed handle for AI/tools (e.g. <c>mailbox:work</c>).</summary>
    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Mailbox, Alias);

    public bool IsDefault { get; init; }

    public EmailProviderPreset Provider { get; init; } = EmailProviderPreset.Custom;

    public string ProviderSlug { get; init; } = "custom";

    public string ProviderName { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;

    public string ImapHost { get; init; } = string.Empty;

    public int ImapPort { get; init; } = 993;

    public bool ImapUseSsl { get; init; } = true;

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 587;

    public bool SmtpUseSsl { get; init; } = true;

    public string Username { get; init; } = string.Empty;

    public bool HasStoredPassword { get; init; }

    public T AsResponse<T>() where T : EmailAccountDto, new() => new()
    {
        Id = Id,
        Alias = Alias,
        IsDefault = IsDefault,
        Provider = Provider,
        ProviderSlug = ProviderSlug,
        ProviderName = ProviderName,
        EmailAddress = EmailAddress,
        ImapHost = ImapHost,
        ImapPort = ImapPort,
        ImapUseSsl = ImapUseSsl,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUseSsl = SmtpUseSsl,
        Username = Username,
        HasStoredPassword = HasStoredPassword
    };
}

public sealed class SaveEmailAccountDto
{
    public Guid? Id { get; init; }

    /// <summary>User-entered alias; omit or leave blank to auto-generate on save.</summary>
    public string Alias { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public string ProviderSlug { get; init; } = "custom";

    public string EmailAddress { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public class EmailSettingsDto
{
    public EmailProviderPreset Provider { get; set; } = EmailProviderPreset.Custom;

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
