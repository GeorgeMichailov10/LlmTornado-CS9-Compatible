using System;
using System.Text.Json.Serialization;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Threads
{

public sealed class Content
{
    [JsonProperty("type")]
    public ContentType Type { get; private set; }

    [JsonProperty("text")] 
    public TextContent Text { get; private set; }

    [JsonProperty("image_file")] 
    public ImageFile ImageFile { get; private set; }

    public override string? ToString()
    {
        return Type switch
        {
            ContentType.Text => Text.Value,
            ContentType.ImageFile => ImageFile.FileId,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
}