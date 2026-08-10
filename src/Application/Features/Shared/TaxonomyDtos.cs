namespace Application.Features.Shared;

public sealed class TagRefDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Color { get; init; }
}

public sealed class BucketRefDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class ErTaxonomyDto
{
    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];
}
