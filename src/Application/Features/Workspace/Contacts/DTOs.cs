namespace Application.Features.Workspace.Contacts;

using Infrastructure.Persistence.Entities;

public sealed class ContactSummaryDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public int SortOrder { get; init; }
}

public class ContactDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Notes { get; init; }

    public ContactSource Source { get; init; }

    public int SortOrder { get; init; }

    public T AsResponse<T>() where T : ContactDto, new() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
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

    public string DisplayName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Notes { get; init; }
}
