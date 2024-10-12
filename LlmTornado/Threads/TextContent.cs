using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.Threads
{

public sealed class TextContent
{
    /// <summary>
    ///     The data that makes up the text.
    /// </summary>
    [JsonInclude]
    [JsonProperty("value")]
    public string Value { get; private set; }

    /// <summary>
    ///     Annotations
    /// </summary>
    [JsonInclude]
    [JsonProperty("annotations")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<Annotation> Annotations { get; private set; }
}
}