namespace Application.Features.workspace.Buckets;

using Application.Features.Shared;

public class BucketDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public BucketRefDto AsRef() => new() { Id = Id, Name = Name };
}

public sealed class SaveBucketDto
{
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
