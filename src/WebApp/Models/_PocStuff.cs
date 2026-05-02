using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Models;


public sealed class ChatThread
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = "New chat";
    public List<ChatMessage> Messages { get; } = [];
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Role { get; init; }
    public required string Content { get; init; }
}

public static class ChatMocks
{
    public static string AssistantReply(string userMessage)
    {
        var preview = userMessage.Length > 90 ? userMessage[..90].Trim() + "…" : userMessage.Trim();
        return $"Thanks — I received: “{preview}”. (Mock reply — edit ChatMocks.AssistantReply in Models/Entities.cs.)";
    }
}


public sealed class ChatMessageDTO
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class SendChatRequestDTO
{
    public string Message { get; set; } = string.Empty;
}

public sealed class SendChatResponseDTO
{
    public string Reply { get; set; } = string.Empty;
}


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


public sealed class SettingsVM
{
    public string ModelName { get; set; } = "demo-model";
    public string Theme { get; set; } = "dark";
}
