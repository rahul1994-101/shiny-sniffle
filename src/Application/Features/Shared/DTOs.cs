namespace Application.Features.Shared;

public sealed class TagRefDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Tag, Alias);

    public string? Color { get; init; }

    public string? Context { get; init; }
}

public sealed class BucketRefDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Bucket, Alias);

    public string? Color { get; init; }

    public string? Context { get; init; }
}

public sealed class ErTaxonomyDto
{
    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];
}

/// <summary>Lightweight row for the <c>@kind:alias</c> mention picker (no taxonomy).</summary>
public sealed class EntityRefMentionItemDto
{
    public EntityRefs.Kind Kind { get; init; }

    public string Alias { get; init; } = string.Empty;

    public string PrimaryLabel { get; init; } = string.Empty;

    public string? SecondaryLabel { get; init; }

    public string? TooltipText { get; init; }

    public string? AvatarText { get; init; }
}
