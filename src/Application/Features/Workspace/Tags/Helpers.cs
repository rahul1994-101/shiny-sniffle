namespace Application.Features.Workspace.Tags;

internal static class TagMapping
{
    internal static string NormalizeName(string name) => name.Trim();

    internal static string? NormalizeAlias(string? value) => EntityAliasRules.SlugifyOptional(value);

    internal static string? ValidateSave(SaveTagDto dto)
    {
        var name = NormalizeName(dto.Name);
        if (string.IsNullOrEmpty(name))
        {
            return "Name is required.";
        }

        if (name.Length > 64)
        {
            return "Name must be 64 characters or fewer.";
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
                && ex.Message.Contains("workspace.Tag", StringComparison.OrdinalIgnoreCase))
            {
                return "Tags storage is not set up. Apply workspace/Tables/Tag.sql on the database.";
            }

            if (ex.Message.Contains("IX_Tag_UserId_Alias", StringComparison.OrdinalIgnoreCase)
                || (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Tag", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Alias", StringComparison.OrdinalIgnoreCase)))
            {
                return "A tag with this alias already exists.";
            }
        }

        return "Could not save tag. Check the database connection and schema scripts.";
    }
}
