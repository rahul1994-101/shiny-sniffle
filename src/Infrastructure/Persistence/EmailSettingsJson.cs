using System.Text.Json;
using System.Text.Json.Serialization;

using Core.Entities;

namespace Infrastructure.Persistence;

internal static class EmailSettingsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    internal static EmailSettings? FromJson(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<EmailSettings>(json, Options);

    internal static string? ToJson(EmailSettings? settings) =>
        settings is null ? null : JsonSerializer.Serialize(settings, Options);
}
