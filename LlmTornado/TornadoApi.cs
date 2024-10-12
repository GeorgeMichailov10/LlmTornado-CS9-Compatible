using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Assistants;
using LlmTornado.Audio;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Code.Vendor;
using LlmTornado.Completions;
using LlmTornado.Embedding;
using LlmTornado.Files;
using LlmTornado.Images;
using LlmTornado.Models;
using LlmTornado.Moderation;
using LlmTornado.Threads;

namespace LlmTornado
{

/// <summary>
///     Entry point to the OpenAPI API, handling auth and allowing access to the various API endpoints
/// </summary>
public class TornadoApi
{
    internal readonly ConcurrentDictionary<LLmProviders, ProviderAuthentication> Authentications = [];
    internal readonly ConcurrentDictionary<LLmProviders, IEndpointProvider> EndpointProviders = [];

    private readonly Lazy<AssistantsEndpoint> assistants;
    private readonly Lazy<AudioEndpoint> audio;
    private readonly Lazy<ChatEndpoint> chat;
    private readonly Lazy<CompletionEndpoint> completion;
    private readonly Lazy<EmbeddingEndpoint> embedding;
    private readonly Lazy<FilesEndpoint> files;
    private readonly Lazy<ImageEditEndpoint> imageEdit;
    private readonly Lazy<ImageGenerationEndpoint> imageGeneration;
    private readonly Lazy<ModelsEndpoint> models;
    private readonly Lazy<ModerationEndpoint> moderation;
    private readonly Lazy<ThreadsEndpoint> threadsEndpoint;
    
    /// <summary>
    ///     Creates a new Tornado API without any authentication. Use this with self-hosted models.
    /// </summary>
    public TornadoApi()
    {
        assistants = new Lazy<AssistantsEndpoint>(() => new AssistantsEndpoint(this));
        audio = new Lazy<AudioEndpoint>(() => new AudioEndpoint(this));
        chat = new Lazy<ChatEndpoint>(() => new ChatEndpoint(this));
        completion = new Lazy<CompletionEndpoint>(() => new CompletionEndpoint(this));
        embedding = new Lazy<EmbeddingEndpoint>(() => new EmbeddingEndpoint(this));
        files = new Lazy<FilesEndpoint>(() => new FilesEndpoint(this));
        imageEdit = new Lazy<ImageEditEndpoint>(() => new ImageEditEndpoint(this));
        imageGeneration = new Lazy<ImageGenerationEndpoint>(() => new ImageGenerationEndpoint(this));
        models = new Lazy<ModelsEndpoint>(() => new ModelsEndpoint(this));
        moderation = new Lazy<ModerationEndpoint>(() => new ModerationEndpoint(this));
        threadsEndpoint = new Lazy<ThreadsEndpoint>(() => new ThreadsEndpoint(this));
    }
    
    /// <summary>
    ///     Creates a new Tornado API with a specific provider authentication. Use when the API will be used only with a single provider.
    /// </summary>
    public TornadoApi(LLmProviders provider, string apiKey, string? organization = null) : this()
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey, organization));
    }
    
    /// <summary>
    ///     Creates a new Tornado API with a specific provider authentication. Use when the API will be used only with a single provider.
    /// </summary>
    public TornadoApi(IEnumerable<ProviderAuthentication> providerKeys) : this()
    {
        foreach (ProviderAuthentication provider in providerKeys)
        {
            Authentications.TryAdd(provider.Provider, provider);
        }
    }

    /// <summary>
    ///     Create a new Tornado API via API key. Use this constructor if in the lifetime of the object only one provider will be used. The API key should match this provider.
    /// </summary>
    /// <param name="apiKey">API key</param>
    /// <param name="provider">Provider</param>
    public TornadoApi(string apiKey, LLmProviders provider = LLmProviders.OpenAi) : this()
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey));
    }

    /// <summary>
    ///     Create a new OpenAiApi via API key and organization key, suitable for (Azure) OpenAI.
    /// </summary>
    /// <param name="apiKey">API key</param>
    /// <param name="organizationKey">Organization key</param>
    /// <param name="provider">Provider</param>
    public TornadoApi(string apiKey, string organizationKey, LLmProviders provider = LLmProviders.OpenAi) : this()
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey, organizationKey));
    }

    /// <summary>
    ///     Gets authentication for a given provider.
    /// </summary>
    /// <returns></returns>
    public ProviderAuthentication? GetProviderAuthentication(LLmProviders provider)
    {
        return Authentications!.GetValueOrDefault(provider, null);
    }
    
    /// <summary>
    ///     Base url for Provider. If null, default specified by the provider is used.
    ///     for OpenAI, should be "https://api.openai.com/{0}/{1}"
    ///     for Azure, should be
    ///     "https://(your-resource-name.openai.azure.com/openai/deployments/(deployment-id)/{1}?api-version={0}"
    ///     this will be formatted as {0} = <see cref="ApiVersion" />, {1} = <see cref="EndpointBase.Endpoint" />
    /// </summary>
    public string? ApiUrlFormat { get; set; }

    /// <summary>
    ///     Version of the Rest Api
    /// </summary>
    public string ApiVersion { get; set; } = "v1";
    
    /// <summary>
    /// Returns a concrete implementation of endpoint provider for a given known provider.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public IEndpointProvider GetProvider(LLmProviders provider)
    {
        if (EndpointProviders.TryGetValue(provider, out IEndpointProvider? p))
        {
            return p;
        }
        
        IEndpointProvider newProvider = EndpointProviderConverter.CreateProvider(provider, this);
        EndpointProviders.TryAdd(provider, newProvider);
        
        return newProvider;
    }
    
    /// <summary>
    /// Returns a concrete implementation of endpoint provider for a given known model.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public IEndpointProvider GetProvider(IModel model)
    {
        return GetProvider(model.Provider);
    }

    /// <summary>
    ///     Interceptor
    /// </summary>
    public Func<ChatRequest, ChatResult?, Task>? ChatRequestInterceptor { get; set; }

    /// <summary>
    ///     The API lets you do operations with images. Given a prompt and an input image, the model will edit a new image.
    /// </summary>
    public ImageEditEndpoint ImageEdit => imageEdit.Value;

    /// <summary>
    ///     Manages audio operations such as transcipt and translate.
    /// </summary>
    public AudioEndpoint Audio => audio.Value;

    /// <summary>
    ///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
    ///     interpreter and file based retrieval.
    /// </summary>
    public AssistantsEndpoint Assistants => assistants.Value;

    /// <summary>
    ///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
    ///     interpreter and file based retrieval.
    /// </summary>
    public ThreadsEndpoint Threads => threadsEndpoint.Value;

    /// <summary>
    ///     Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way
    ///     you “program” the API to do a task is by simply describing the task in plain english or providing a few written
    ///     examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar
    ///     correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
    /// </summary>
    public CompletionEndpoint Completions => completion.Value;

    /// <summary>
    ///     The API lets you transform text into a vector (list) of floating point numbers. The distance between two vectors
    ///     measures their relatedness. Small distances suggest high relatedness and large distances suggest low relatedness.
    /// </summary>
    public EmbeddingEndpoint Embeddings => embedding.Value;

    /// <summary>
    ///     Text generation in the form of chat messages. This interacts with the ChatGPT API.
    /// </summary>
    public ChatEndpoint Chat => chat.Value;

    /// <summary>
    ///     Classify text against the OpenAI Content Policy.
    /// </summary>
    public ModerationEndpoint Moderation => moderation.Value;

    /// <summary>
    ///     The API endpoint for querying available Engines/models
    /// </summary>
    public ModelsEndpoint Models => models.Value;

    /// <summary>
    ///     The API lets you do operations with files. You can upload, delete or retrieve files. Files can be used for
    ///     fine-tuning, search, etc.
    /// </summary>
    public FilesEndpoint Files => files.Value;

    /// <summary>
    ///     The API lets you do operations with images. Given a prompt and/or an input image, the model will generate a new
    ///     image.
    /// </summary>
    public ImageGenerationEndpoint ImageGenerations => imageGeneration.Value;
}
}