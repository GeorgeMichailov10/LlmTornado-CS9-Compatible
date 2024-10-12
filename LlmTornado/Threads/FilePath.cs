using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.Threads
{

public sealed class FilePath
{
    /// <summary>
    ///     The ID of the file that was generated.
    /// </summary>
    [JsonInclude]
    [JsonProperty("file_id")]
    public string FileId { get; private set; }
}
}