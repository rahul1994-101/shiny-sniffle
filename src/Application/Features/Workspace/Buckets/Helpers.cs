namespace Application.Features.Workspace.Buckets;

internal static class BucketMapping
{
    internal static string NormalizeName(string name) => name.Trim();

    internal static string? NormalizeAlias(string? value) => EntityAliasRules.SlugifyOptional(value);

    internal static string? ValidateSave(SaveBucketDto dto)
    {
        var name = NormalizeName(dto.Name);
        if (string.IsNullOrEmpty(name))
        {
            return "Name is required.";
        }

        if (name.Length > 128)
        {
            return "Name must be 128 characters or fewer.";
        }

        var alias = NormalizeAlias(dto.Alias);
        if (alias is not null && alias.Length > EntityAliasRules.MaxLength)
        {
            return "Alias must be 64 characters or fewer.";
        }

        var colorError = CatalogFieldRules.ValidateColor(dto.Color);
        if (colorError is not null)
        {
            return colorError;
        }

        if (dto.Context is not null && dto.Context.Trim().Length > CatalogFieldRules.ContextMaxLength)
        {
            return $"Context must be {CatalogFieldRules.ContextMaxLength} characters or fewer.";
        }

        return null;
    }

    internal static string MapSaveError(Exception exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
                && ex.Message.Contains("workspace.Bucket", StringComparison.OrdinalIgnoreCase))
            {
                return "Buckets storage is not set up. Apply workspace/Tables/Bucket.sql on the database.";
            }

            if (ex.Message.Contains("IX_Bucket_UserId_Alias", StringComparison.OrdinalIgnoreCase)
                || (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Bucket", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Alias", StringComparison.OrdinalIgnoreCase)))
            {
                return "A bucket with this alias already exists.";
            }
        }

        return "Could not save bucket. Check the database connection and schema scripts.";
    }
}
