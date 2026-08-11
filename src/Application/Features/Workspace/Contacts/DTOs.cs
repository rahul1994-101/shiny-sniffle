namespace Application.Features.workspace.Contacts;

public sealed class ContactSummaryDto
{
    public Guid Id { get; init; }

    public string ListLabel { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Contact, Alias);

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public int SortOrder { get; init; }

    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];
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

    public int SortOrder { get; init; }

    public IReadOnlyList<TagRefDto> Tags { get; init; } = [];

    public IReadOnlyList<BucketRefDto> Buckets { get; init; } = [];

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
        SortOrder = SortOrder,
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
