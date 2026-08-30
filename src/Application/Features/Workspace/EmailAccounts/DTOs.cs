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

    public StoredMailboxSettings Connection { get; init; } = new();

    public bool HasStoredPassword { get; init; }

    public string? Context { get; init; }

    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];

    public string EmailAddress => Connection.EmailAddress;

    public string ImapHost => Connection.ImapHost;

    public int ImapPort => Connection.ImapPort;

    public bool ImapUseSsl => Connection.ImapUseSsl;

    public string SmtpHost => Connection.SmtpHost;

    public int SmtpPort => Connection.SmtpPort;

    public bool SmtpUseSsl => Connection.SmtpUseSsl;

    public string Username => Connection.Username;

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
            Connection = settings,
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
        Connection = new StoredMailboxSettings
        {
            EmailAddress = Connection.EmailAddress,
            ImapHost = Connection.ImapHost,
            ImapPort = Connection.ImapPort,
            ImapUseSsl = Connection.ImapUseSsl,
            SmtpHost = Connection.SmtpHost,
            SmtpPort = Connection.SmtpPort,
            SmtpUseSsl = Connection.SmtpUseSsl,
            Username = Connection.Username,
            Password = Connection.Password
        },
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

/// <summary>UI/draft mailbox settings — provider selection wraps merged <see cref="StoredMailboxSettings"/>.</summary>
public class EmailSettingsDto
{
    public Guid EmailProviderId { get; set; }

    public bool HasStoredPassword { get; set; }

    public StoredMailboxSettings Settings { get; set; } = new();

    public string EmailAddress { get => Settings.EmailAddress; set => Settings.EmailAddress = value; }

    public string ImapHost { get => Settings.ImapHost; set => Settings.ImapHost = value; }

    public int ImapPort { get => Settings.ImapPort; set => Settings.ImapPort = value; }

    public bool ImapUseSsl { get => Settings.ImapUseSsl; set => Settings.ImapUseSsl = value; }

    public string SmtpHost { get => Settings.SmtpHost; set => Settings.SmtpHost = value; }

    public int SmtpPort { get => Settings.SmtpPort; set => Settings.SmtpPort = value; }

    public bool SmtpUseSsl { get => Settings.SmtpUseSsl; set => Settings.SmtpUseSsl = value; }

    public string Username { get => Settings.Username; set => Settings.Username = value; }

    public string Password { get => Settings.Password; set => Settings.Password = value; }

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
        HasStoredPassword = HasStoredPassword,
        Settings = new StoredMailboxSettings
        {
            EmailAddress = Settings.EmailAddress,
            Username = Settings.Username,
            ImapHost = Settings.ImapHost,
            ImapPort = Settings.ImapPort,
            ImapUseSsl = Settings.ImapUseSsl,
            SmtpHost = Settings.SmtpHost,
            SmtpPort = Settings.SmtpPort,
            SmtpUseSsl = Settings.SmtpUseSsl,
            Password = Settings.Password
        }
    };

    public T AsResponse<T>() where T : EmailSettingsDto, new() => new()
    {
        EmailProviderId = EmailProviderId,
        HasStoredPassword = HasStoredPassword,
        Settings = new StoredMailboxSettings
        {
            EmailAddress = Settings.EmailAddress,
            ImapHost = Settings.ImapHost,
            ImapPort = Settings.ImapPort,
            ImapUseSsl = Settings.ImapUseSsl,
            SmtpHost = Settings.SmtpHost,
            SmtpPort = Settings.SmtpPort,
            SmtpUseSsl = Settings.SmtpUseSsl,
            Username = Settings.Username,
            Password = Settings.Password
        }
    };
}
