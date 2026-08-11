namespace Application.Features.workspace.Buckets;

internal static class BucketMapping
{
    internal static BucketDto ToDto(Bucket entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Alias = entity.Alias,
        Color = entity.Color,
        Context = entity.Context,
        SortOrder = entity.SortOrder
    };

    internal static string NormalizeName(string name) => name.Trim();

    internal static string? NormalizeAlias(string? value) => EntityAliasRules.NormalizeOptional(value);

    internal static string? NormalizeContext(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > 2000 ? trimmed[..2000] : trimmed;
    }

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

        if (dto.Context is not null && dto.Context.Trim().Length > 2000)
        {
            return "Context must be 2000 characters or fewer.";
        }

        return null;
    }
}
