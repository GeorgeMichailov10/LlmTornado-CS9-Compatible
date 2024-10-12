﻿using System;
using Newtonsoft.Json;

namespace LlmTornado.Images
{

/// <summary>
///     Represents available sizes for image generation endpoints
/// </summary>
public class ImageSize
{
    private ImageSize(string value)
    {
        Value = value;
    }

    private string Value { get; }

    /// <summary>
    ///     Requests an image that is 256x256
    /// </summary>
    public static ImageSize _256 => new("256x256");

    /// <summary>
    ///     Requests an image that is 512x512
    /// </summary>
    public static ImageSize _512 => new("512x512");

    /// <summary>
    ///     Requests and image that is 1024x1024
    /// </summary>
    public static ImageSize _1024 => new("1024x1024");

    /// <summary>
    ///     Requests and image that is 1792x1024, only for dalle3
    /// </summary>
    public static ImageSize _1792x1024 => new("1792x1024");

    /// <summary>
    ///     Requests and image that is 1024x1792
    /// </summary>
    public static ImageSize _1024x1792 => new("1024x1792");

    /// <summary>
    ///     Gets the string value for this size to pass to the API
    /// </summary>
    /// <returns>The size as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this size to pass to the API
    /// </summary>
    /// <param name="value">The ImageSize to convert</param>
    public static implicit operator string(ImageSize value)
    {
        return value.Value;
    }

    internal class ImageSizeJsonConverter : JsonConverter<ImageSize>
    {
        public override void WriteJson(JsonWriter writer, ImageSize value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ImageSize ReadJson(JsonReader reader, Type objectType, ImageSize existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new ImageSize(reader.ReadAsString());
        }
    }
}
}