using Infrastructure.Persistence.workspace;

namespace Application.Features.workspace.Tags;

internal static class TagMapping
{
    internal static TagDto ToDto(Tag entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Color = entity.Color,
        SortOrder = entity.SortOrder
    };

    internal static string NormalizeName(string name) => name.Trim();

    internal static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        if (trimmed.Length is > 9)
        {
            return trimmed[..9];
        }

        return trimmed;
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

        return null;
    }
}
