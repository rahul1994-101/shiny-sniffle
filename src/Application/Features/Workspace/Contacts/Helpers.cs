namespace Application.Features.Workspace.Contacts;

internal static class ContactMapping
{
    internal static string ResolveListLabel(Contact entity) =>
        FormatFullName(entity.FirstName, entity.LastName);

    internal static string FormatFullName(string firstName, string lastName)
    {
        var first = firstName.Trim();
        var last = lastName.Trim();
        return string.IsNullOrEmpty(last) ? first : $"{first} {last}";
    }

    internal static string? NormalizeAlias(string? value) => EntityAliasRules.SlugifyOptional(value);

    internal static string BuildAliasStem(string firstName, string lastName) =>
        EntityAliasRules.StemFromPersonName(firstName, lastName);

    internal static string AliasWithNumericSuffix(string stem, int index) =>
        EntityAliasRules.WithNumericSuffix(stem, index, "contact");

    internal static string? ValidateSave(SaveContactDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName))
        {
            return "First name is required.";
        }

        if (dto.FirstName.Trim().Length > 50)
        {
            return "First name must be 50 characters or fewer.";
        }

        if (dto.LastName.Trim().Length > 50)
        {
            return "Last name must be 50 characters or fewer.";
        }

        var alias = NormalizeAlias(dto.Alias);
        if (alias is not null && alias.Length > EntityAliasRules.MaxLength)
        {
            return "Alias must be 64 characters or fewer.";
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

        if (dto.Context is not null && dto.Context.Trim().Length > 2000)
        {
            return "Context must be 2000 characters or fewer.";
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
                return "Contacts storage is not set up. Apply workspace/Tables/Contact.sql on the database.";
            }

            if (ex.Message.Contains("IX_Contact_UserId_Email", StringComparison.OrdinalIgnoreCase)
                || (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Contact", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Email", StringComparison.OrdinalIgnoreCase)))
            {
                return "A contact with this email already exists.";
            }

            if (ex.Message.Contains("IX_Contact_UserId_Alias", StringComparison.OrdinalIgnoreCase)
                || (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Contact", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("Alias", StringComparison.OrdinalIgnoreCase)))
            {
                return "A contact with this alias already exists.";
            }
        }

        return "Could not save contact. Check the database connection and schema scripts.";
    }
}

/// <summary>Public surface for contact alias preview (UI).</summary>
public static class ContactAliases
{
    public static string StemFromName(string firstName, string lastName) =>
        EntityAliasRules.StemFromPersonName(firstName, lastName);
}
