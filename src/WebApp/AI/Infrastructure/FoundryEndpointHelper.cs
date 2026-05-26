namespace WebApp.AI.Infrastructure;

public static class FoundryEndpointHelper
{
    /// <summary>
    /// Maps a Foundry project endpoint to the OpenAI-compatible <c>/openai/v1</c> data-plane URL used with API keys.
    /// </summary>
    public static Uri GetOpenAiV1Endpoint(string projectEndpoint, string? openAiEndpointOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(openAiEndpointOverride))
        {
            return NormalizeOpenAiEndpoint(openAiEndpointOverride);
        }

        if (!Uri.TryCreate(projectEndpoint, UriKind.Absolute, out var projectUri))
        {
            throw new InvalidOperationException("Foundry:ProjectEndpoint is not a valid absolute URL.");
        }

        return new Uri($"{projectUri.Scheme}://{projectUri.Authority}/openai/v1/");
    }

    private static Uri NormalizeOpenAiEndpoint(string endpoint)
    {
        var trimmed = endpoint.Trim();
        if (!trimmed.EndsWith('/'))
        {
            trimmed += "/";
        }

        return new Uri(trimmed, UriKind.Absolute);
    }
}
