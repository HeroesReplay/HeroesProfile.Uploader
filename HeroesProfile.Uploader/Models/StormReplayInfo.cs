using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using Heroes.StormReplayParser;
using ReactiveUI;

namespace HeroesProfile.Uploader.Models;

public class StoredReplayInfo
{
    [JsonPropertyName("filePath")] public string? FilePath { get; set; }

    [JsonPropertyName("created")] public DateTime Created { get; set; }

    [JsonPropertyName("uploadStatus")] public UploadStatus UploadStatus { get; set; }

    public StormReplayInfo ToStormReplayInfo() => new StormReplayInfo { FilePath = FilePath!, Created = Created, UploadStatus = UploadStatus };
}

public class StormReplayInfo : ReactiveObject, IEquatable<StormReplayInfo>
{
    #region Calculated Properties

    [JsonIgnore] public StormReplay? StormReplay { get; set; }

    [JsonIgnore] public string? Fingerprint { get; set; }

    [JsonIgnore] public string FileName => Path.GetFileNameWithoutExtension(FilePath);

    [JsonIgnore] public bool IsSuccess => UploadStatus == UploadStatus.Success;

    [JsonIgnore]
    public bool IsWarning => UploadStatus != UploadStatus.InProgress && UploadStatus != UploadStatus.Success && UploadStatus != UploadStatus.UploadError;

    [JsonIgnore] public bool IsError => UploadStatus == UploadStatus.UploadError;

    #endregion

    [JsonPropertyName("filePath")] public string FilePath { get; set; } = null!;
    [JsonPropertyName("created")] public DateTime Created { get; set; }

    [JsonPropertyName("deleted")] public bool Deleted { get; set; } = false;

    UploadStatus _uploadStatus = UploadStatus.Pending;


    [JsonPropertyName("uploadStatus")]
    public UploadStatus UploadStatus
    {
        get => _uploadStatus;
        set {
            this.RaiseAndSetIfChanged(ref _uploadStatus, value);
            this.RaisePropertyChanged(nameof(IsSuccess));
            this.RaisePropertyChanged(nameof(IsWarning));
            this.RaisePropertyChanged(nameof(IsError));
        }
    }

    public StormReplayInfo()
    {
    }

    public StoredReplayInfo ToStorageReplay() => new() { FilePath = FilePath, Created = Created, UploadStatus = UploadStatus };

    public int CompareTo(StormReplayInfo? other)
    {
        if (other == null)
            return 1;

        return string.Compare(FilePath, other.FilePath, StringComparison.Ordinal);
    }

    public bool Equals(StormReplayInfo? other)
    {
        return other != null && FilePath.Equals(other.FilePath);
    }

    public override string ToString() => FilePath;
}