namespace Infrastructure.Utilities.Helpers;

public static class EmailSettingsJsonHelpers
{
    public static EmailSettings? FromJson(string? json) =>
        JsonColumnHelpers.Deserialize<EmailSettings>(json);

    public static string? ToJson(EmailSettings? settings) =>
        JsonColumnHelpers.Serialize(settings);
}
