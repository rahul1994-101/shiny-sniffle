namespace Application.Features.dbo.EmailProviders;

public class EmailProviderDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ImapHost { get; init; } = string.Empty;

    public int ImapPort { get; init; }

    public bool ImapUseSsl { get; init; }

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; }

    public bool SmtpUseSsl { get; init; }

    public string? SetupHelpUrl { get; init; }

    public int SortOrder { get; init; }

    public bool IsSystem { get; init; }

    public T AsResponse<T>() where T : EmailProviderDto, new() => new()
    {
        Id = Id,
        Name = Name,
        Slug = Slug,
        ImapHost = ImapHost,
        ImapPort = ImapPort,
        ImapUseSsl = ImapUseSsl,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUseSsl = SmtpUseSsl,
        SetupHelpUrl = SetupHelpUrl,
        SortOrder = SortOrder,
        IsSystem = IsSystem
    };
}

public sealed class SaveEmailProviderDto
{
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ImapHost { get; init; } = string.Empty;

    public int ImapPort { get; init; } = 993;

    public bool ImapUseSsl { get; init; } = true;

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 587;

    public bool SmtpUseSsl { get; init; } = true;

    public string? SetupHelpUrl { get; init; }

    public int SortOrder { get; init; }
}
