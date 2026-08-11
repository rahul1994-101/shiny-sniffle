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
        SortOrder = entity.SortOrder,
        IsSystem = entity.IsSystem
    };

    internal static string? ValidateSave(SaveEmailProviderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required.";
        }

        return CatalogFieldRules.ValidateSlug(dto.Slug, required: false);
    }

    internal static async Task<string> ResolveSlugAsync(
        Func<string, Guid?, CancellationToken, Task<bool>> isTakenAsync,
        string displayName,
        string? requestedSlug,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        await WorkspaceErAliasResolver.ResolveAsync(
            isTakenAsync,
            displayName,
            requestedSlug,
            excludeId,
            "provider",
            cancellationToken);
}
