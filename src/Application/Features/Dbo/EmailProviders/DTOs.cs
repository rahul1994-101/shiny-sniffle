namespace Application.Features.Dbo.EmailProviders;

public class EmailProviderDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string ImapHost { get; init; } = string.Empty;

    public int ImapPort { get; init; }

    public bool ImapUseSsl { get; init; }

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; }

    public bool SmtpUseSsl { get; init; }

    public bool IsSystem { get; init; }

    public static EmailProviderDto FromEntity(EmailProvider entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ImapHost = entity.ImapHost,
        ImapPort = entity.ImapPort,
        ImapUseSsl = entity.ImapUseSsl,
        SmtpHost = entity.SmtpHost,
        SmtpPort = entity.SmtpPort,
        SmtpUseSsl = entity.SmtpUseSsl,
        IsSystem = entity.IsSystem
    };

    public EmailProvider ToEntity() => new()
    {
        Id = Id,
        Name = Name,
        ImapHost = ImapHost,
        ImapPort = ImapPort,
        ImapUseSsl = ImapUseSsl,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUseSsl = SmtpUseSsl,
        IsSystem = IsSystem
    };

    public T AsResponse<T>() where T : EmailProviderDto, new() => new()
    {
        Id = Id,
        Name = Name,
        ImapHost = ImapHost,
        ImapPort = ImapPort,
        ImapUseSsl = ImapUseSsl,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUseSsl = SmtpUseSsl,
        IsSystem = IsSystem
    };
}

public sealed class SaveEmailProviderDto
{
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string ImapHost { get; init; } = string.Empty;

    public int ImapPort { get; init; } = 993;

    public bool ImapUseSsl { get; init; } = true;

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 587;

    public bool SmtpUseSsl { get; init; } = true;
}
