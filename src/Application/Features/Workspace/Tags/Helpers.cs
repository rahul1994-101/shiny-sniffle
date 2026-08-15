namespace Application.Features.Workspace.Tags;

internal static class TagMapping
{
    internal static string NormalizeName(string name) => name.Trim();

    internal static string? NormalizeAlias(string? value) => EntityAliasRules.SlugifyOptional(value);

    internal static string? NormalizeContext(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > 2000 ? trimmed[..2000] : trimmed;
    }

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

        if (dto.Context is not null && dto.Context.Trim().Length > 2000)
        {
            return "Context must be 2000 characters or fewer.";
        }

        return null;
    }
}
