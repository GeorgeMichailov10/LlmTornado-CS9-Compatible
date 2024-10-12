using System.Text.Json.Serialization;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads
{

public sealed class CodeInterpreterOutputs
{
    /// <summary>
    ///     Output type. Can be either 'logs' or 'image'.
    /// </summary>
    [JsonProperty("type")]
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    public CodeInterpreterOutputType Type { get; private set; }

    /// <summary>
    ///     Text output from the Code Interpreter tool call as part of a run step.
    /// </summary>
    [JsonProperty("logs")]
    public string Logs { get; private set; }

    /// <summary>
    ///     Code interpreter image output.
    /// </summary>
    [JsonProperty("image")]
    public ImageFile Image { get; private set; }
}
}