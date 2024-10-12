using LlmTornado.Code.Vendor;

namespace LlmTornado.Code
{

internal static class EndpointProviderConverter
{
    public static IEndpointProvider CreateProvider(LLmProviders provider, TornadoApi api)
    {
        return provider switch
        {
            LLmProviders.OpenAi => new OpenAiEndpointProvider(api),
            LLmProviders.Anthropic => new AnthropicEndpointProvider(api),
            LLmProviders.Cohere => new CohereEndpointProvider(api),
            LLmProviders.Google => new GoogleEndpointProvider(api),
            LLmProviders.Groq => new OpenAiEndpointProvider(api, LLmProviders.Groq)
            {
                UrlResolver = (endpoint, url) => $"{string.Format(api.ApiUrlFormat ?? "https://api.groq.com/openai/{0}/{1}", api.ApiVersion, OpenAiEndpointProvider.GetEndpointUrlFragment(endpoint))}{url}"
            },
            _ => new OpenAiEndpointProvider(api)
        };
    }
}
}