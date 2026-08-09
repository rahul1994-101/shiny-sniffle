using Infrastructure.Persistence.Entities.dbo;
using System.Text.RegularExpressions;

namespace Application.Features.EmailProviders;

internal static partial class EmailProviderMapping
{
    internal static EmailProviderDto FromEntity(EmailProvider entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Slug = entity.Slug,
        ImapHost = entity.ImapHost,
        ImapPort = entity.ImapPort,
        ImapUseSsl = entity.ImapUseSsl,
        SmtpHost = entity.SmtpHost,
        SmtpPort = entity.SmtpPort,
        SmtpUseSsl = entity.SmtpUseSsl,
        SetupHelpUrl = entity.SetupHelpUrl,
        SortOrder = entity.SortOrder,
        IsSystem = entity.IsSystem
    };

    internal static string? ValidateSave(SaveEmailProviderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(dto.Slug))
        {
            return "Slug is required.";
        }

        if (!SlugRegex().IsMatch(dto.Slug.Trim()))
        {
            return "Slug must use lowercase letters, numbers, and hyphens only.";
        }

        if (dto.ImapPort is < 1 or > 65535 || dto.SmtpPort is < 1 or > 65535)
        {
            return "Ports must be between 1 and 65535.";
        }

        return null;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();
}
