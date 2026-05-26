namespace WebApp.AI.Infrastructure;

public static class FoundryEndpointHelper
{
    /// <summary>
    /// Normalizes AZURE_OPENAI_ENDPOINT to the base resource URL expected by <see cref="Azure.AI.OpenAI.AzureOpenAIClient"/>.
    /// Accepts either a base URL or a full deployment chat/completions URL.
    /// </summary>
    public static Uri GetAzureOpenAiEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Foundry:Endpoint is not configured.");
        }

        var trimmed = endpoint.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Foundry:Endpoint is not a valid absolute URL.");
        }

        var baseUrl = ExtractBaseUrl(uri);
        return NormalizeTrailingSlash(baseUrl);
    }

    private static string ExtractBaseUrl(Uri uri)
    {
        var path = uri.AbsolutePath;

        var deploymentsIndex = path.IndexOf("/openai/deployments/", StringComparison.OrdinalIgnoreCase);
        if (deploymentsIndex >= 0)
        {
            return $"{uri.Scheme}://{uri.Authority}{path[..deploymentsIndex]}/";
        }

        var openAiIndex = path.IndexOf("/openai/", StringComparison.OrdinalIgnoreCase);
        if (openAiIndex >= 0)
        {
            return $"{uri.Scheme}://{uri.Authority}{path[..openAiIndex]}/";
        }

        return $"{uri.Scheme}://{uri.Authority}/";
    }

    private static Uri NormalizeTrailingSlash(string endpoint)
    {
        var trimmed = endpoint.Trim();
        if (!trimmed.EndsWith('/'))
        {
            trimmed += "/";
        }

        return new Uri(trimmed, UriKind.Absolute);
    }
}
