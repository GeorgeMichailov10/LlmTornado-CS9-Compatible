using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Completions;
using Newtonsoft.Json;
using LlmTornado.Code.Models;
using LlmTornado;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Vendor.Anthropic;

namespace LlmTornado.Chat
{

/// <summary>
///     A request to the Chat API. This is similar, but not exactly the same as the
///     <see cref="Completions.CompletionRequest" />
///     Based on the <see href="https://platform.openai.com/docs/api-reference/chat">OpenAI API docs</see>
/// </summary>
public class ChatRequest
{
	/// <summary>
	///     Creates a new, empty <see cref="ChatRequest" />
	/// </summary>
	public ChatRequest()
    {
    }

	/// <summary>
	///     Create a new chat request using the data from the input chat request.
	/// </summary>
	/// <param name="basedOn"></param>
	public ChatRequest(ChatRequest? basedOn)
    {
	    if (basedOn is null)
	    {
		    return;
	    }

        Model = basedOn.Model;
        Messages = basedOn.Messages;
        Temperature = basedOn.Temperature;
        TopP = basedOn.TopP;
        NumChoicesPerMessage = basedOn.NumChoicesPerMessage;
        StopSequence = basedOn.StopSequence;
        MultipleStopSequences = basedOn.MultipleStopSequences;
        MaxTokens = basedOn.MaxTokens;
        FrequencyPenalty = basedOn.FrequencyPenalty;
        PresencePenalty = basedOn.PresencePenalty;
        LogitBias = basedOn.LogitBias;
        Tools = basedOn.Tools;
        ToolChoice = basedOn.ToolChoice;
        OutboundFunctionsContent = basedOn.OutboundFunctionsContent;
        Adapter = basedOn.Adapter;
        VendorExtensions = basedOn.VendorExtensions;
        StreamOptions = basedOn.StreamOptions;
        TrimResponseStart = basedOn.TrimResponseStart;
        ParallelToolCalls = basedOn.ParallelToolCalls;
        Seed = basedOn.Seed;
        User = basedOn.User;
        ResponseFormat = basedOn.ResponseFormat;
    }

	/// <summary>
	///     The model to use for this request
	/// </summary>
	[JsonProperty("model")]
	[JsonConverter(typeof(ModelJsonConverter))]
	public ChatModel? Model { get; set; } = ChatModel.OpenAi.Gpt35.Turbo;

	/// <summary>
	///     The messages to send with this Chat Request
	/// </summary>
	[JsonProperty("messages")]
    [JsonConverter(typeof(ChatMessageRequestMessagesJsonConverter))]
    public IList<ChatMessage>? Messages { get; set; }

	/// <summary>
	///     What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative
	///     applications, and 0 (argmax sampling) for ones with a well-defined answer. It is generally recommend to use this or
	///     <see cref="TopP" /> but not both.
	/// </summary>
	[JsonProperty("temperature")]
    public double? Temperature { get; set; }

	/// <summary>
	///     An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the
	///     tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are
	///     considered. It is generally recommend to use this or <see cref="Temperature" /> but not both.
	/// </summary>
	[JsonProperty("top_p")]
    public double? TopP { get; set; }

	/// <summary>
	///     How many different choices to request for each message. Defaults to 1.
	/// </summary>
	[JsonProperty("n")]
    public int? NumChoicesPerMessage { get; set; }

	/// <summary>
	///     The seed to use for deterministic requests.
	/// </summary>
	[JsonProperty("seed")]
    public int? Seed { get; set; }

	/// <summary>
	///     The response format to use. If <see cref="ChatRequestResponseFormats.Json" />, either system or user message in the
	///     conversation must contain "JSON".
	/// </summary>
	[JsonProperty("response_format")]
    public ChatRequestResponseFormats? ResponseFormat { get; set; }

	/// <summary>
	///     Specifies the response should be streamed. This is set automatically by the library.
	/// </summary>
	[JsonProperty("stream")]
    public bool Stream { get; internal set; }

	/// <summary>
	///     The stream configuration.
	/// </summary>
	[JsonIgnore]
	public ChatStreamOptions? StreamOptions
	{
		get => StreamOptionsInternal;
		set
		{
			StreamOptionsInternal = value;
			StreamOptionsInternalSerialized = StreamOptionsInternal;
		}
	}

	[JsonIgnore]
	internal ChatStreamOptions? StreamOptionsInternal { get; set; }
	
	[JsonProperty("stream_options")]
	internal object? StreamOptionsInternalSerialized { get; set; }
	
	/// <summary>
	///     This is only used for serializing the request into JSON, do not use it directly.
	/// </summary>
	[JsonProperty("stop")]
    internal object? CompiledStop
    {
        get
        {
            return MultipleStopSequences?.Length switch
            {
                1 => StopSequence,
                > 0 => MultipleStopSequences,
                _ => null
            };
        }
    }

	/// <summary>
	///     One or more sequences where the API will stop generating further tokens. The returned text will not contain the
	///     stop sequence.
	/// </summary>
	[JsonIgnore]
    public string[]? MultipleStopSequences { get; set; }

	/// <summary>
	///     The stop sequence where the API will stop generating further tokens. The returned text will not contain the stop
	///     sequence.  For convenience, if you are only requesting a single stop sequence, set it here
	/// </summary>
	[JsonIgnore]
    public string? StopSequence
    {
        get => MultipleStopSequences?.FirstOrDefault() ?? null;
        set
        {
            if (value != null)
                MultipleStopSequences = [value];
        }
    }

	/// <summary>
	///     How many tokens to complete to. Can return fewer if a stop sequence is hit.
	/// </summary>
	[JsonProperty("max_tokens")]
    public int? MaxTokens { get; set; }

	/// <summary>
	///		Strategy for serializing <see cref="MaxTokens"/>.
	/// </summary>
	[JsonIgnore] 
	public ChatRequestMaxTokensSerializers MaxTokensSerializer { get; set; } = ChatRequestMaxTokensSerializers.Auto;

	/// <summary>
	///     The scale of the penalty for how often a token is used.  Should generally be between 0 and 1, although negative
	///     numbers are allowed to encourage token reuse.  Defaults to 0.
	/// </summary>
	[JsonProperty("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

	/// <summary>
	///     The scale of the penalty applied if a token is already present at all.  Should generally be between 0 and 1,
	///     although negative numbers are allowed to encourage token reuse.  Defaults to 0.
	/// </summary>
	[JsonProperty("presence_penalty")]
    public double? PresencePenalty { get; set; }

	/// <summary>
	///     Modify the likelihood of specified tokens appearing in the completion.
	///     Accepts a json object that maps tokens(specified by their token ID in the tokenizer) to an associated bias value
	///     from -100 to 100.
	///     Mathematically, the bias is added to the logits generated by the model prior to sampling.
	///     The exact effect will vary per model, but values between -1 and 1 should decrease or increase likelihood of
	///     selection; values like -100 or 100 should result in a ban or exclusive selection of the relevant token.
	/// </summary>
	[JsonProperty("logit_bias")]
    public IReadOnlyDictionary<string, float>? LogitBias { get; set; }

	/// <summary>
	///     A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
	/// </summary>
	[JsonProperty("user")]
    public string? User { get; set; }

	/// <summary>
	///     A list of tools the model may generate JSON inputs for.
	/// </summary>
	[JsonProperty("tools")]
    public List<Tool>? Tools { get; set; }

	/// <summary>
	///     Parallel function calling can be disabled / enabled for vendors supporting the feature.
	///		As of 6/24, the only vendor supporting the feature is OpenAI.
	/// </summary>
	[JsonProperty("parallel_tool_calls")]
	public bool? ParallelToolCalls { get; set; }

	/// <summary>
	///     Represents an optional field when sending tools calling prompt.
	///     This field determines which function to call.
	/// </summary>
	/// <remarks>
	///     If this field is not specified, the default behavior ("auto") allows the model to automatically decide whether to
	///     call tools or not.
	///     Specify the name of the function to call in the "Name" attribute of the FunctionCall object.
	///     If you do not want the model to call any function, pass "None" for the "Name" attribute.
	/// </remarks>
	[JsonProperty("tool_choice")]
    [JsonConverter(typeof(OutboundToolChoice.OutboundToolChoiceConverter))]
    public OutboundToolChoice? ToolChoice { get; set; }

	/// <summary>
	///     If set the functions part of the outbound request encoded as JSON are stored here.
	///     This can be used a cheap heuristic for counting tokens used when streaming.
	///     Note that OpenAI silently transforms the provided JSON-schema into TypeScript and hence the real usage will be
	///     somewhat lower.
	/// </summary>
	[JsonIgnore]
    public Ref<string>? OutboundFunctionsContent { get; internal set; }

	/// <summary>
	///     This can be any API provider specific data. Currently used in KoboldCpp.
	/// </summary>
	[JsonProperty("adapter")]
    public Dictionary<string, object?>? Adapter { get; set; }
	
	/// <summary>
	///		Features supported only by a single/few providers with no shared equivalent.
	/// </summary>
	[JsonIgnore]
	public ChatRequestVendorExtensions? VendorExtensions { get; set; }

	/// <summary>
	///		Trims the leading whitespace and newline characters in the response. Unless you need to work with responses
	///		with leading whitespace it is recommended to keep this switch on. When streaming, some providers/models incorrectly
	///		produce leading whitespace if the text part of the streamed response is preceded with tool blocks.
	/// </summary>
	[JsonIgnore]
	public bool TrimResponseStart { get; set; } = true;

	/// <summary>
	///		Cancellation token to use with the request.
	/// </summary>
	[JsonIgnore]
	public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
	
	[JsonIgnore]
	internal string? UrlOverride { get; set; }

	internal void OverrideUrl(string url)
	{
		UrlOverride = url;
	}
	
	private static readonly PropertyRenameAndIgnoreSerializerContractResolver MaxTokensRenamer = new PropertyRenameAndIgnoreSerializerContractResolver();
	private static readonly JsonSerializerSettings MaxTokensRenamerSettings = new JsonSerializerSettings
	{
		ContractResolver = MaxTokensRenamer,
		NullValueHandling = NullValueHandling.Ignore
	};
	
	static ChatRequest()
	{
		MaxTokensRenamer.RenameProperty(typeof(ChatRequest), "max_tokens", "max_completion_tokens");	
	}

	private static readonly Dictionary<LLmProviders, Func<ChatRequest, IEndpointProvider, string>> SerializeMap = new Dictionary<LLmProviders, Func<ChatRequest, IEndpointProvider, string>>
	{
		{ LLmProviders.OpenAi, (x, y) =>
			{
				switch (x.MaxTokensSerializer)
				{
					case ChatRequestMaxTokensSerializers.Auto:
					{
						if (x.Model is not null)
						{
							if (ChatModelOpenAiGpt4.ReasoningModels.Contains(x.Model))
							{
								return JsonConvert.SerializeObject(x, MaxTokensRenamerSettings);
							}	
						}

						return JsonConvert.SerializeObject(x, EndpointBase.NullSettings);
					}
					case ChatRequestMaxTokensSerializers.MaxCompletionTokens:
					{
						return JsonConvert.SerializeObject(x, MaxTokensRenamerSettings);
					}
					default:
					{
						return JsonConvert.SerializeObject(x, EndpointBase.NullSettings);
					}
				}
			}
		},
		{ LLmProviders.Anthropic, (x, y) => JsonConvert.SerializeObject(new VendorAnthropicChatRequest(x, y), EndpointBase.NullSettings) },
		{ LLmProviders.Cohere, (x, y) => JsonConvert.SerializeObject(new VendorCohereChatRequest(x, y), EndpointBase.NullSettings) },
		{ LLmProviders.Google, (x, y) => JsonConvert.SerializeObject(new VendorGoogleChatRequest(x, y), EndpointBase.NullSettings) },
		{ LLmProviders.Groq, (x, y) =>
			{
				// fields unsupported by groq
				x.LogitBias = null; 
				return JsonConvert.SerializeObject(x, EndpointBase.NullSettings);
			} 
		}
	};

	/// <summary>
	///		Serializes the chat request into the request body, based on the conventions used by the LLM provider.
	/// </summary>
	/// <param name="provider"></param>
	/// <returns></returns>
	public TornadoRequestContent Serialize(IEndpointProvider provider)
	{
		return SerializeMap.TryGetValue(provider.Provider, out Func<ChatRequest, IEndpointProvider, string>? serializerFn) ? new TornadoRequestContent(serializerFn.Invoke(this, provider), UrlOverride) : new TornadoRequestContent(string.Empty, UrlOverride);
	}
	
	internal class ModelJsonConverter : JsonConverter<ChatModel>
	{
		public override void WriteJson(JsonWriter writer, ChatModel? value, JsonSerializer serializer)
		{
			writer.WriteValue(value?.GetApiName);
		}

		public override ChatModel? ReadJson(JsonReader reader, Type objectType, ChatModel? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return existingValue;
		}
	}
    
    internal class ChatMessageRequestMessagesJsonConverter : JsonConverter<IList<ChatMessage>?>
    {
        public override void WriteJson(JsonWriter writer, IList<ChatMessage>? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();

            foreach (ChatMessage msg in value)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("role");
                writer.WriteValue(msg.rawRole);

                if (msg.Role is not null)
                {
	                switch (msg.Role)
	                {
		                case ChatMessageRoles.Tool:
		                {
			                writer.WritePropertyName("tool_call_id");
			                writer.WriteValue(msg.ToolCallId);
			                break;
		                }
		                case ChatMessageRoles.Assistant:
		                {
			                if (msg.ToolCalls is not null)
			                {
				                writer.WritePropertyName("tool_calls");

				                writer.WriteStartArray();

				                foreach (ToolCall call in msg.ToolCalls)
				                {
					                writer.WriteStartObject();

					                writer.WritePropertyName("id");
					                writer.WriteValue(call.Id);

					                writer.WritePropertyName("type");
					                writer.WriteValue(call.Type);

					                writer.WritePropertyName("function");
					                writer.WriteStartObject();

					                writer.WritePropertyName("name");
					                writer.WriteValue(call.FunctionCall.Name);

					                writer.WritePropertyName("arguments");
					                writer.WriteValue(call.FunctionCall.Arguments);

					                writer.WriteEndObject();

					                writer.WriteEndObject();
				                }

				                writer.WriteEndArray();
			                }

			                break;
		                }
	                }
                }

                if (!string.IsNullOrWhiteSpace(msg.Name))
                {
                    writer.WritePropertyName("name");
                    writer.WriteValue(msg.Name);
                }

                if (msg is { Role: ChatMessageRoles.Tool, Content: null })
                {
	                goto closeMsgObj;
                }

                if (msg is { Role: ChatMessageRoles.Assistant, Content: null, ToolCalls: not null })
                {
	                goto closeMsgObj;
                }
                
                writer.WritePropertyName("content");

                if (msg.Parts?.Count > 0)
                {
                    writer.WriteStartArray();

                    foreach (ChatMessagePart part in msg.Parts)
                    {
                        writer.WriteStartObject();

                        writer.WritePropertyName("type");

                        string type = part.Type switch
                        {
	                        ChatMessageTypes.Text => "text",
	                        ChatMessageTypes.Image => "image_url",
	                        ChatMessageTypes.Audio => "audio_url",
	                        _ => "text"
                        };
                        
	                    writer.WriteValue(type);   
	                    
                        switch (part.Type)
                        {
	                        case ChatMessageTypes.Text:
	                        {
		                        writer.WritePropertyName("text");
		                        writer.WriteValue(part.Text);
		                        break;
	                        }
	                        case ChatMessageTypes.Image:
	                        {
		                        writer.WritePropertyName("image_url");
		                        writer.WriteStartObject();

		                        writer.WritePropertyName("url");
		                        writer.WriteValue(part.Image?.Url);

		                        writer.WriteEndObject();
		                        break;
	                        }
                        }

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }
                else
                {
                    writer.WriteValue(msg.Content);
                }

                closeMsgObj:
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override IList<ChatMessage>? ReadJson(JsonReader reader, Type objectType, IList<ChatMessage>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return existingValue;
        }
    }
}
}