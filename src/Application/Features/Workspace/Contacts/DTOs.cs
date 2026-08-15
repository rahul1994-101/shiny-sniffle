namespace Application.Features.Workspace.Contacts;

public sealed class ContactSummaryDto
{
    public Guid Id { get; init; }

    public string ListLabel { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Contact, Alias);

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];

    public static ContactSummaryDto FromEntity(Contact entity, ErTaxonomyDto? taxonomy = null) => new()
    {
        Id = entity.Id,
        ListLabel = ContactMapping.ResolveListLabel(entity),
        Alias = entity.Alias,
        Email = entity.Email,
        Phone = entity.Phone,
        Tags = taxonomy?.Tags ?? [],
        Buckets = taxonomy?.Buckets ?? []
    };
}

public class ContactDto
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Contact, Alias);

    public string ListLabel { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Context { get; init; }

    public ContactSource Source { get; init; }

    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];

    public static ContactDto FromEntity(Contact entity, ErTaxonomyDto? taxonomy = null) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Alias = entity.Alias,
        ListLabel = ContactMapping.ResolveListLabel(entity),
        Email = entity.Email,
        Phone = entity.Phone,
        Context = entity.Context,
        Source = entity.Source,
        Tags = taxonomy?.Tags ?? [],
        Buckets = taxonomy?.Buckets ?? []
    };

    public T AsResponse<T>() where T : ContactDto, new() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        Alias = Alias,
        ListLabel = ListLabel,
        Email = Email,
        Phone = Phone,
        Context = Context,
        Source = Source,
        Tags = Tags,
        Buckets = Buckets
    };
}

public sealed class SaveContactDto
{
    public Guid? Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    /// <summary>User-entered alias; omit or leave blank to auto-generate on save.</summary>
    public string? Alias { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Context { get; init; }

    public IReadOnlyList<Guid> TagIds { get; init; } = [];

    public IReadOnlyList<Guid> BucketIds { get; init; } = [];
}
