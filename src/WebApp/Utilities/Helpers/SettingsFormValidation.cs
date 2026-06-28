using System.ComponentModel.DataAnnotations;

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

    public static string? ValidateFirstName(string firstName) =>
        ValidateMember(new SaveGeneralProfileRequest
        {
            UserId = Guid.Empty,
            FirstName = firstName,
            LastName = "ab"
        }, nameof(SaveGeneralProfileRequest.FirstName));

    public static string? ValidateLastName(string lastName) =>
        ValidateMember(new SaveGeneralProfileRequest
        {
            UserId = Guid.Empty,
            FirstName = "ab",
            LastName = lastName
        }, nameof(SaveGeneralProfileRequest.LastName));

    public static string? ValidatePasswordChange(string current, string newPassword, string confirm)
    {
        if (!string.Equals(newPassword, confirm, StringComparison.Ordinal))
        {
            return "New password and confirmation do not match.";
        }

        var request = new ChangePasswordRequest
        {
            UserId = Guid.Empty,
            CurrentPassword = current,
            NewPassword = newPassword,
            ConfirmPassword = confirm
        };

        return FirstValidationMessage(
            request,
            nameof(ChangePasswordRequest.CurrentPassword),
            nameof(ChangePasswordRequest.NewPassword),
            nameof(ChangePasswordRequest.ConfirmPassword));
    }

    public static bool EmailSettingsEqual(EmailSettingsDto left, EmailSettingsDto right) =>
        left.Provider == right.Provider
        && string.Equals(left.EmailAddress, right.EmailAddress, StringComparison.Ordinal)
        && string.Equals(left.Username, right.Username, StringComparison.Ordinal)
        && string.Equals(left.ImapHost, right.ImapHost, StringComparison.Ordinal)
        && left.ImapPort == right.ImapPort
        && left.ImapUseSsl == right.ImapUseSsl
        && string.Equals(left.SmtpHost, right.SmtpHost, StringComparison.Ordinal)
        && left.SmtpPort == right.SmtpPort
        && left.SmtpUseSsl == right.SmtpUseSsl
        && left.HasStoredPassword == right.HasStoredPassword;

    public static EmailSettingsDto CloneEmailSettings(EmailSettingsDto source) =>
        new()
        {
            Provider = source.Provider,
            EmailAddress = source.EmailAddress,
            Username = source.Username,
            ImapHost = source.ImapHost,
            ImapPort = source.ImapPort,
            ImapUseSsl = source.ImapUseSsl,
            SmtpHost = source.SmtpHost,
            SmtpPort = source.SmtpPort,
            SmtpUseSsl = source.SmtpUseSsl,
            HasStoredPassword = source.HasStoredPassword,
            Password = string.Empty
        };

    private static string? FirstValidationMessage(object instance, params string[] memberNames)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);

        if (Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
        {
            return null;
        }

        foreach (var result in results)
        {
            if (result.MemberNames.Any(m => memberNames.Contains(m, StringComparer.Ordinal)))
            {
                return result.ErrorMessage;
            }
        }

        return results.FirstOrDefault()?.ErrorMessage;
    }

    private static string? ValidateMember(object instance, string memberName)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance) { MemberName = memberName };
        var property = instance.GetType().GetProperty(memberName);
        if (property is null)
        {
            return null;
        }

        var value = property.GetValue(instance);
        if (Validator.TryValidateProperty(value, context, results))
        {
            return null;
        }

        return results.FirstOrDefault()?.ErrorMessage;
    }
}
