namespace WebApp.Components.Shared;

/// <summary>
/// Shared alias field labels and hints (Contacts, Email accounts).
    /// <see cref="OptionalMarker"/> means optional <em>user input</em>; the database column is always NOT NULL (app auto-fills).
    /// </summary>
public static class EntityAliasFieldCopy
{
    public const string OptionalMarker = FormFieldCopy.OptionalMarker;

    public const string EntityRefLead = "AI reference:";

    public static string EmptyFieldHint(string autoGenerateFrom, string scopePlural) =>
        $"Short handle for agents and settings (stored as a lowercase slug). Leave blank to auto-generate from {autoGenerateFrom}; unique among your {scopePlural}.";

    public const string PlaceholderAutoFromName = "Leave blank to auto-generate from name";

    public const string PlaceholderAutoFromEmail = "Leave blank to auto-generate from email address";
}
