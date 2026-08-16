using Application.Features.Shared;

namespace WebApp.Components.Shared;

/// <summary>
/// Shared alias field labels and hints (Contacts, Email accounts).
/// <see cref="OptionalMarker"/> means optional <em>user input</em>; the database column is always NOT NULL (app auto-fills).
/// </summary>
public static class EntityAliasFieldCopy
{
    public const string OptionalMarker = FormFieldCopy.OptionalMarker;

    public const string PlaceholderAuto = "auto";

    public static string AutoGenerateSourceLabel(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "first and last name",
        EntityRefs.Kind.Mailbox => "email address",
        EntityRefs.Kind.Tag or EntityRefs.Kind.Bucket => "name",
        _ => "name"
    };

    public static string ScopePlural(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => "contacts",
        EntityRefs.Kind.Mailbox => "mailboxes",
        EntityRefs.Kind.Tag => "tags",
        EntityRefs.Kind.Bucket => "buckets",
        _ => "items"
    };

    public static string EmptyFieldHint(EntityRefs.Kind kind) =>
        EmptyFieldHint(AutoGenerateSourceLabel(kind), ScopePlural(kind));

    public static string EmptyFieldHint(string autoGenerateFrom, string scopePlural) =>
        $"Short handle for agents and settings (stored as a lowercase slug). Leave blank to auto-generate from {autoGenerateFrom}; unique among your {scopePlural}.";
}
