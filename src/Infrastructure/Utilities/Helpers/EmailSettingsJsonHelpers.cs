namespace Infrastructure.Utilities.Helpers;

internal static class EmailSettingsJsonHelpers
{
    internal static EmailSettings? FromJson(string? json) =>
        JsonColumnHelpers.Deserialize<EmailSettings>(json);

    internal static string? ToJson(EmailSettings? settings) =>
        JsonColumnHelpers.Serialize(settings);
}
