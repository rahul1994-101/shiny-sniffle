namespace Application.Features.Workspace.EmailAccounts;

using Infrastructure.Mailbox;

public sealed class EmailAccountSummaryDto
{
    public Guid Id { get; init; }

    public string Alias { get; init; } = string.Empty;

    /// <summary>Typed handle for AI/tools (e.g. <c>mailbox:work</c>).</summary>
    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Mailbox, Alias);

    public string ProviderName { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];

    public static EmailAccountSummaryDto FromEntity(
        EmailAccount account,
        EmailProvider provider,
        ErTaxonomyDto? taxonomy = null) => new()
    {
        Id = account.Id,
        Alias = account.Alias,
        ProviderName = provider.Name,
        EmailAddress = account.EmailAddress,
        IsDefault = account.IsDefault,
        Tags = taxonomy?.Tags ?? [],
        Buckets = taxonomy?.Buckets ?? []
    };
}

/// <summary>Resolved workspace mailbox account ready for <see cref="IMailboxService"/> calls.</summary>
public sealed class MailboxAccountContext
{
    public Guid EmailAccountId { get; init; }

    public string Alias { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public EmailSettings Runtime { get; init; } = null!;
}

public sealed class MailboxAccountResolveResult
{
    public MailboxAccountContext? Context { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => Context is not null;

    internal static MailboxAccountResolveResult Ok(MailboxAccountContext context) =>
        new() { Context = context };

    internal static MailboxAccountResolveResult Fail(string message) =>
        new() { ErrorMessage = message };
}

public class EmailAccountDto
{
    public Guid Id { get; init; }

    public string Alias { get; init; } = string.Empty;

    /// <summary>Typed handle for AI/tools (e.g. <c>mailbox:work</c>).</summary>
    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Mailbox, Alias);

    public bool IsDefault { get; init; }

    public Guid EmailProviderId { get; init; }

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

    public string? Context { get; init; }

    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];

    public static EmailAccountDto FromEntity(
        EmailAccount account,
        EmailProvider provider,
        ErTaxonomyDto? taxonomy = null)
    {
        var settings = EmailAccountMapping.ToStoredSettings(account, provider);
        return new EmailAccountDto
        {
            Id = account.Id,
            Alias = account.Alias,
            IsDefault = account.IsDefault,
            EmailProviderId = provider.Id,
            ProviderName = provider.Name,
            EmailAddress = settings.EmailAddress,
            ImapHost = settings.ImapHost,
            ImapPort = settings.ImapPort,
            ImapUseSsl = settings.ImapUseSsl,
            SmtpHost = settings.SmtpHost,
            SmtpPort = settings.SmtpPort,
            SmtpUseSsl = settings.SmtpUseSsl,
            Username = settings.Username,
            HasStoredPassword = !string.IsNullOrWhiteSpace(settings.Password),
            Context = account.Context,
            Tags = taxonomy?.Tags ?? [],
            Buckets = taxonomy?.Buckets ?? []
        };
    }

    public T AsResponse<T>() where T : EmailAccountDto, new() => new()
    {
        Id = Id,
        Alias = Alias,
        IsDefault = IsDefault,
        EmailProviderId = EmailProviderId,
        ProviderName = ProviderName,
        EmailAddress = EmailAddress,
        ImapHost = ImapHost,
        ImapPort = ImapPort,
        ImapUseSsl = ImapUseSsl,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUseSsl = SmtpUseSsl,
        Username = Username,
        HasStoredPassword = HasStoredPassword,
        Context = Context,
        Tags = Tags,
        Buckets = Buckets
    };
}

public sealed class SaveEmailAccountDto
{
    public Guid? Id { get; init; }

    /// <summary>User-entered alias; omit or leave blank to auto-generate on save.</summary>
    public string Alias { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public Guid EmailProviderId { get; init; }

    public string EmailAddress { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? Context { get; init; }

    public IReadOnlyList<Guid> TagIds { get; init; } = [];

    public IReadOnlyList<Guid> BucketIds { get; init; } = [];
}

/// <summary>
/// Merged workspace mailbox connection settings (account + provider catalog).
/// Password is stored encrypted — map to <see cref="EmailSettings"/> via <see cref="EmailSettingsMapping.ToMailRuntime"/> before calling <see cref="IMailboxService"/>.
/// </summary>
public sealed class StoredMailboxSettings
{
    public string EmailAddress { get; set; } = string.Empty;

    public string ImapHost { get; set; } = string.Empty;

    public int ImapPort { get; set; } = 993;

    public bool ImapUseSsl { get; set; } = true;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class EmailSettingsDto
{
    public Guid EmailProviderId { get; set; }

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
        EmailProviderId == other.EmailProviderId
        && string.Equals(EmailAddress, other.EmailAddress, StringComparison.Ordinal)
        && string.Equals(Username, other.Username, StringComparison.Ordinal)
        && HasStoredPassword == other.HasStoredPassword;

    private EmailSettingsDto CloneShallow() => new()
    {
        EmailProviderId = EmailProviderId,
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
        EmailProviderId = EmailProviderId,
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
