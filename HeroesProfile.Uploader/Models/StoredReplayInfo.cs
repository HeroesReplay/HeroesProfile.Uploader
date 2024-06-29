using System;
using System.Text.Json.Serialization;

namespace HeroesProfile.Uploader.Models;

public class StoredReplayInfo
{
    [JsonPropertyName("filePath")] public required string FilePath { get; init; }

    [JsonPropertyName("created")] public required DateTime Created { get; init; }

    [JsonPropertyName("uploadStatus")] public required UploadStatus UploadStatus { get; init; }

    public StormReplayInfo ToStormReplayInfo() => new() { FilePath = FilePath, Created = Created, UploadStatus = UploadStatus };
}