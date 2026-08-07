using Application.Features.EmailProviders;

namespace Application.Features.UserSettings;

internal static class EmailSettingsCatalog
{
    internal static async Task<(IReadOnlyList<EmailProviderDto> Catalog, string? Error)> LoadCatalogAsync(
        EmailProviderRepository emailProviderRepo,
        CancellationToken cancellationToken)
    {
        var catalog = await emailProviderRepo.ListAsync(cancellationToken);
        if (catalog.Count == 0)
        {
            return (catalog, "No mail providers are configured. Add templates under Settings → Email → Providers.");
        }

        return (catalog, null);
    }

    internal static string? TryApplyCatalog(EmailSettingsDto dto, IReadOnlyList<EmailProviderDto> catalog)
    {
        var row = EmailProviderCatalog.FindBySlug(catalog, dto.ProviderSlug);
        if (row is null)
        {
            return "Select a valid mail provider.";
        }

        var endpointError = EmailProviderCatalog.ValidateCatalogEndpoints(row);
        if (endpointError is not null)
        {
            return endpointError;
        }

        EmailProviderCatalog.ApplyTo(dto, row);
        return null;
    }
}
