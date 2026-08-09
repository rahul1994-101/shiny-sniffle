namespace Application.Features.Workspace.Contacts;

using Application.Features.Shared;
using Infrastructure.Persistence.Entities;

public sealed class ContactSummaryDto
{
    public Guid Id { get; init; }

    public string ListLabel { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    /// <summary>Typed handle for AI/tools (e.g. <c>contact:jane-smith</c>).</summary>
    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Contact, Alias);

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public int SortOrder { get; init; }
}

public class ContactDto
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;

    /// <summary>Typed handle for AI/tools (e.g. <c>contact:jane-smith</c>).</summary>
    public string EntityRef => EntityRefs.Format(EntityRefs.Kind.Contact, Alias);

    public string ListLabel { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Notes { get; init; }

    public ContactSource Source { get; init; }

    public int SortOrder { get; init; }

    public T AsResponse<T>() where T : ContactDto, new() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        Alias = Alias,
        ListLabel = ListLabel,
        Email = Email,
        Phone = Phone,
        Notes = Notes,
        Source = Source,
        SortOrder = SortOrder
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

    public string? Notes { get; init; }
}
