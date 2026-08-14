namespace Application.Features.Workspace.Buckets;

public class BucketDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Bucket, Alias);

    public string? Color { get; init; }

    public string? Context { get; init; }

    public int SortOrder { get; init; }

    public static BucketDto FromEntity(Bucket entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Alias = entity.Alias,
        Color = entity.Color,
        Context = entity.Context,
        SortOrder = entity.SortOrder
    };

    public BucketRefDto AsRef() => new()
    {
        Id = Id,
        Name = Name,
        Alias = Alias,
        Color = Color,
        Context = Context
    };

    public T AsResponse<T>() where T : BucketDto, new() => new()
    {
        Id = Id,
        Name = Name,
        Alias = Alias,
        Color = Color,
        Context = Context,
        SortOrder = SortOrder
    };
}

public sealed class SaveBucketDto
{
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Alias { get; init; }

    public string? Color { get; init; }

    public string? Context { get; init; }
}
