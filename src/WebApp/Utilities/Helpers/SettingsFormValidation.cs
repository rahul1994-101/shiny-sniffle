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

    public static string? ValidateMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return null;
        }

        if (mobile.Length > 20)
        {
            return "Mobile must be 20 characters or fewer.";
        }

        return null;
    }

    public static string? ValidateOptionalPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        if (password.Length is < 6 or > 255)
        {
            return "Password must be between 6 and 255 characters.";
        }

        return null;
    }
}
