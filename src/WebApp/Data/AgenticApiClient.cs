using System.Net.Http.Json;
using System.Text.Json;
using WebApp.Models;

namespace WebApp.Data;

public sealed class AgenticApiClient
{
    private static string FormatHttpError(System.Net.HttpStatusCode status, string raw)
    {
        var snippet = raw.Length <= 1200 ? raw : raw[..1200] + "...";
        return $"HTTP {(int)status}: {snippet}";
    }

    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        // FastAPI / Pydantic expect snake_case JSON keys (message, user_email, …).
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        // Avoid culture-dependent number formatting if we add decimals later.
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public AgenticApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(bool Ok, string Reply, string? Error)> MailAgentChatAsync(
        string baseUrl,
        string message,
        string? userEmail,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{baseUrl.TrimEnd('/')}/mail_agent_chat";
        var body = new MailAgentChatApiRequest { Message = message, UserEmail = userEmail };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            using var response = await client.PostAsJsonAsync(url, body, JsonWrite, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (false, "", FormatHttpError(response.StatusCode, raw));
            }

            var env = JsonSerializer.Deserialize<ServiceEnvelopeDto>(raw, JsonRead);
            if (env is null)
            {
                return (false, "", "Invalid JSON from agent API.");
            }

            if (env.HasError)
            {
                return (false, "", ServiceEnvelopeDto.FormatErrors(env.Errors));
            }

            return (true, ServiceEnvelopeDto.FormatPayload(env.Payload), null);
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

    public async Task<(bool Ok, string? Error, string? StoragePath)> StoreGmailTokensAsync(
        string baseUrl,
        string email,
        string? refreshToken,
        string? accessToken,
        int? expiresInSeconds,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{baseUrl.TrimEnd('/')}/gmail/store_tokens";
        var body = new GmailStoreTokensApiRequest
        {
            Email = email,
            RefreshToken = refreshToken,
            AccessToken = accessToken,
            ExpiresInSeconds = expiresInSeconds,
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(1);
            using var response = await client.PostAsJsonAsync(url, body, JsonWrite, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (false, FormatHttpError(response.StatusCode, raw), null);
            }

            var env = JsonSerializer.Deserialize<ServiceEnvelopeDto>(raw, JsonRead);
            if (env is null)
            {
                return (false, "Invalid JSON from agent API.", null);
            }

            if (env.HasError)
            {
                return (false, ServiceEnvelopeDto.FormatErrors(env.Errors), null);
            }

            var path = TryGetPayloadString(env.Payload, "storage_path");
            return (true, null, path);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    private static string? TryGetPayloadString(JsonElement payload, string property)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return payload.TryGetProperty(property, out var el)
            ? el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString()
            : null;
    }
}
