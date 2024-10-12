using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads
{

[JsonConverter(typeof(StringEnumConverter))]
public enum AnnotationType
{
    [EnumMember(Value = "file_citation")] FileCitation,
    [EnumMember(Value = "file_path")] FilePath
}
}