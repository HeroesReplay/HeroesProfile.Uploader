using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using HeroesProfile.Uploader.Core.JsonConverters;

namespace HeroesProfile.Uploader.Models;

public class UploadResult
{
    [JsonPropertyName("fingerprint")]
    public Guid Fingerprint { get; set; }

    [JsonPropertyName("replayID")]
    public int ReplayId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    public static UploadResult? FromJson(string json) => JsonSerializer.Deserialize<UploadResult>(json, Converter.Settings);
    
    public string ToJson() => JsonSerializer.Serialize(this, Converter.Settings);
}

internal static class Converter
{
    public static readonly JsonSerializerOptions? Settings = new() {

        Converters = { 
            new IsoDateTimeConverter(DateTimeStyles.AssumeUniversal)
        },
    };
}
