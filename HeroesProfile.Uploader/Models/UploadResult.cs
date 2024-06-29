using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeroesProfile.Uploader.Models;

public class UploadResult
{
    [JsonPropertyName("fingerprint")]
    public required Guid Fingerprint { get; init; }

    [JsonPropertyName("replayID")]
    public required int ReplayId { get; init; }

    [JsonPropertyName("status")] 
    public required UploadStatus Status { get; init; }
    
    public UploadResult()
    {
        
    }
}
