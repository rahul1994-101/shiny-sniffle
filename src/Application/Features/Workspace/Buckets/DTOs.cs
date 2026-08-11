namespace Application.Features.workspace.Buckets;

using Application.Features.Shared;

public class BucketDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Bucket, Alias);

    public string? Color { get; init; }

    public string? Context { get; init; }

    public int SortOrder { get; init; }

    public BucketRefDto AsRef() => new()
    {
        Id = Id,
        Name = Name,
        Alias = Alias,
        Color = Color,
        Context = Context
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
