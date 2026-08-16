namespace WebApp.Components.Shared;

/// <summary>Shared password field hints (profile + mailbox credentials).</summary>
public static class PasswordFieldCopy
{
    public const string StoredEncrypted = "Stored encrypted.";

    public const string LeaveBlankToKeep = "Leave blank on save to keep the saved password.";

    public static string ProfileHint => $"{StoredEncrypted} {LeaveBlankToKeep}";

    public static string MailboxHint(bool isNew) =>
        isNew
            ? $"{StoredEncrypted} Required when adding an account."
            : $"{StoredEncrypted} {LeaveBlankToKeep}";
}
