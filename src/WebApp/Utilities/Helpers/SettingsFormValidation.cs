namespace WebApp.Utilities.Helpers;

public static class SettingsFormValidation
{
    public static string? ValidateProfileNames(string firstName, string lastName)
    {
        var first = ValidateFirstName(firstName);
        if (first is not null)
        {
            return first;
        }

        return ValidateLastName(lastName);
    }

    public static string? ValidateFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return "First name is required.";
        }

        if (firstName.Length is < 2 or > 50)
        {
            return "First name must be between 2 and 50 characters.";
        }

        return null;
    }

    public static string? ValidateLastName(string lastName)
    {
        if (string.IsNullOrWhiteSpace(lastName))
        {
            return "Last name is required.";
        }

        if (lastName.Length is < 2 or > 50)
        {
            return "Last name must be between 2 and 50 characters.";
        }

        return null;
    }

    public static string? ValidatePasswordChange(string current, string newPassword, string confirm)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return "Current password is required.";
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return "New password is required.";
        }

        if (newPassword.Length is < 6 or > 255)
        {
            return "New password must be between 6 and 255 characters.";
        }

        if (string.IsNullOrWhiteSpace(confirm))
        {
            return "Confirm password is required.";
        }

        if (!string.Equals(newPassword, confirm, StringComparison.Ordinal))
        {
            return "New password and confirmation do not match.";
        }

        return null;
    }
}
