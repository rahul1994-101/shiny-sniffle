namespace Application.Features.EmailAccounts;

public sealed class EmailAccountSummaryDto
{
    public Guid Id { get; init; }

    public string Alias { get; init; } = string.Empty;

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

    public bool IsDefault { get; init; }

    public EmailProvider Provider { get; init; } = EmailProvider.Custom;

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

    public string Alias { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public string ProviderSlug { get; init; } = "custom";

    public string EmailAddress { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
