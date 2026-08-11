namespace Application.Features.Shared;

/// <summary>Preview handles in workspace forms before save (matches server slugify rules).</summary>
public static class EntityAliasPreview
{
    public static string FromNameOrAlias(string? alias, string displayName, string emptyFallback)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            return EntityAliasRules.StemFromLabel(alias.Trim(), emptyFallback);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return string.Empty;
        }

        return EntityAliasRules.StemFromLabel(displayName.Trim(), emptyFallback);
    }
}
