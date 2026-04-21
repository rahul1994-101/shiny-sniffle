using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Models;

public sealed class AgenticApiOptions
{
    public const string SectionName = "AgenticApi";

    /// <summary>Base URL of the FastAPI service (no trailing slash required).</summary>
    public string BaseUrl { get; set; } = "";
}

public sealed class MailAgentChatApiRequest
{
    /// <summary>Serialized as "message" (camelCase policy).</summary>
    public string Message { get; set; } = "";

    /// <summary>Explicit snake_case; policy would emit "userEmail" otherwise.</summary>
    [JsonPropertyName("user_email")]
    public string? UserEmail { get; set; }
}

public sealed class GmailStoreTokensApiRequest
{
    /// <summary>Serialized as "email".</summary>
    public string Email { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expires_in_seconds")]
    public int? ExpiresInSeconds { get; set; }
}

public sealed class ServiceEnvelopeDto
{
    public bool HasError { get; set; }

    public JsonElement Errors { get; set; }

    public JsonElement Payload { get; set; }

    public static string FormatErrors(JsonElement errors)
    {
        return errors.ValueKind switch
        {
            JsonValueKind.String => errors.GetString() ?? "",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => errors.ToString(),
        };
    }

    public static string FormatPayload(JsonElement payload)
    {
        return payload.ValueKind switch
        {
            JsonValueKind.String => payload.GetString() ?? "",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => payload.ToString(),
        };
    }
}
