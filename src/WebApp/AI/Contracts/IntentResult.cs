using System.Text.Json.Serialization;

namespace WebApp.AI.Contracts;

public sealed class IntentResult
{
    [JsonPropertyName("intent")]
    public string Intent { get; set; } = IntentKeys.GeneralChat;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
