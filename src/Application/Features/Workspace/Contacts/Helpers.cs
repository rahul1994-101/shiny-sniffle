using Infrastructure.Persistence.Entities;

namespace Application.Features.Workspace.Contacts;

internal static class ContactMapping
{
    internal static ContactSummaryDto ToSummary(Contact entity) => new()
    {
        Id = entity.Id,
        DisplayName = entity.DisplayName,
        Email = entity.Email,
        Phone = entity.Phone,
        SortOrder = entity.SortOrder
    };

    internal static ContactDto ToDto(Contact entity) => new()
    {
        Id = entity.Id,
        DisplayName = entity.DisplayName,
        Email = entity.Email,
        Phone = entity.Phone,
        Notes = entity.Notes,
        Source = entity.Source,
        SortOrder = entity.SortOrder
    };

    internal static string? ValidateSave(SaveContactDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName))
        {
            return "Display name is required.";
        }

        if (dto.DisplayName.Trim().Length > 200)
        {
            return "Display name must be 200 characters or fewer.";
        }

        var email = NormalizeEmail(dto.Email);
        if (email is not null && email.Length > 255)
        {
            return "Email must be 255 characters or fewer.";
        }

        var phone = NormalizePhone(dto.Phone);
        if (phone is not null && phone.Length > 32)
        {
            return "Phone must be 32 characters or fewer.";
        }

        if (dto.Notes is not null && dto.Notes.Trim().Length > 2000)
        {
            return "Notes must be 2000 characters or fewer.";
        }

        return null;
    }

    internal static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant();
    }

    internal static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    internal static string MapSaveError(Exception exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
                && ex.Message.Contains("workspace.Contact", StringComparison.OrdinalIgnoreCase))
            {
                return "Contacts storage is not set up. Apply Persistence/Schema/workspace/Tables/Contact.sql on the database.";
            }

            if (ex.Message.Contains("IX_Contact_UserId_Email", StringComparison.OrdinalIgnoreCase)
                || (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Contact", StringComparison.OrdinalIgnoreCase)))
            {
                return "A contact with this email already exists.";
            }
        }

        return "Could not save contact. Check the database connection and schema scripts.";
    }
}
