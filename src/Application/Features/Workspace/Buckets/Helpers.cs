using Infrastructure.Persistence.workspace;

namespace Application.Features.workspace.Buckets;

internal static class BucketMapping
{
    internal static BucketDto ToDto(Bucket entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        SortOrder = entity.SortOrder
    };

    internal static string NormalizeName(string name) => name.Trim();

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

        return null;
    }
}
