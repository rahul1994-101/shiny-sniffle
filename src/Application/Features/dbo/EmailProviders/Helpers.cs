using Application.Features.Shared;
using Infrastructure.Persistence.dbo;

namespace Application.Features.dbo.EmailProviders;

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
        Color = entity.Color,
        Note = entity.Note,
        SortOrder = entity.SortOrder,
        IsSystem = entity.IsSystem
    };

    internal static string? ValidateSave(SaveEmailProviderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required.";
        }

        return CatalogFieldRules.ValidateSlug(dto.Slug, required: true);
    }
}
