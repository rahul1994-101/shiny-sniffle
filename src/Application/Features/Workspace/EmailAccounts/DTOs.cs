namespace Application.Features.Workspace.EmailAccounts;

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
        ProviderSlug = provider.Slug,
        EmailAddress = account.EmailAddress,
        IsDefault = account.IsDefault,
        SortOrder = account.SortOrder,
        Tags = taxonomy?.Tags ?? [],
        Buckets = taxonomy?.Buckets ?? []
    };
}

public class EmailAccountDto
{
    public Guid Id { get; init; }

    public string Alias { get; init; } = string.Empty;

    /// <summary>Typed handle for AI/tools (e.g. <c>mailbox:work</c>).</summary>
    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Mailbox, Alias);

    public bool IsDefault { get; init; }

    public string ProviderSlug { get; init; } = string.Empty;

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
        var settings = EmailAccountMapping.ToEmailSettings(account, provider);
        return new EmailAccountDto
        {
            Id = account.Id,
            Alias = account.Alias,
            IsDefault = account.IsDefault,
            ProviderSlug = settings.ProviderSlug,
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

    public string ProviderSlug { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? Context { get; init; }

    public IReadOnlyList<Guid> TagIds { get; init; } = [];

    public IReadOnlyList<Guid> BucketIds { get; init; } = [];
}

public class EmailSettingsDto
{
    public string ProviderSlug { get; set; } = string.Empty;

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
