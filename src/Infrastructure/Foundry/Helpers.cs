using Azure.AI.OpenAI;

namespace Infrastructure.Foundry;

/// <summary>
/// Normalizes Azure OpenAI / Foundry endpoint URLs from config (resource base URL, trailing slash).
/// </summary>
internal static class AzureOpenAiEndpointHelpers
{
    internal static AzureOpenAIClientOptions? CreateClientOptions(string apiVersion)
    {
        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            return null;
        }

        var enumName = "V" + apiVersion.Trim().Replace("-", "_", StringComparison.Ordinal);
        if (!Enum.TryParse<AzureOpenAIClientOptions.ServiceVersion>(enumName, ignoreCase: true, out var version))
        {
            throw new InvalidOperationException(
                $"Foundry:ApiVersion '{apiVersion}' is not a supported Azure OpenAI API version.");
        }

        return new AzureOpenAIClientOptions(version);
    }

    internal static Uri ToAzureOpenAiBaseUri(string endpoint)
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
        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }

        return new Uri(baseUrl, UriKind.Absolute);
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
}
