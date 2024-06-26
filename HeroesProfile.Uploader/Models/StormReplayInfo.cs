using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using Heroes.StormReplayParser;
using HeroesProfile.Uploader.Core.Enums;

namespace HeroesProfile.Uploader.Models;

[Serializable]
public class StormReplayInfo : INotifyPropertyChanged, IComparable<StormReplayInfo>
{
    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonIgnore]
    public string? Fingerprint { get; set; }
    
    [JsonIgnore]
    public string FileName => Path.GetFileNameWithoutExtension(FilePath);

    [JsonPropertyName("filePath")] public string FilePath { get; set; } = null!;

    [JsonPropertyName("created")] public DateTime Created { get; set; }
    
    [JsonIgnore]
    public StormReplay? StormReplay { get; set; }

    private bool _deleted;
    public bool Deleted
    {
        get => _deleted;
        set {
            if (_deleted == value) {
                return;
            }

            _deleted = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Deleted)));
        }
    }

    UploadStatus _uploadStatus = UploadStatus.None;

    public UploadStatus UploadStatus
    {
        get => _uploadStatus;
        set {
            if (_uploadStatus == value) {
                return;
            }

            _uploadStatus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UploadStatus)));
        }
    }

    [JsonIgnore]
    public bool IsSuccess => UploadStatus == UploadStatus.Success;
    
    [JsonIgnore]
    public bool IsInProgress => UploadStatus == UploadStatus.InProgress;
    
    [JsonIgnore]
    public bool IsUploadError => UploadStatus == UploadStatus.UploadError;
    
    [JsonIgnore]
    public bool IsDuplicate => UploadStatus == UploadStatus.Duplicate;
    
    [JsonIgnore]
    public bool IsAiDetected => UploadStatus == UploadStatus.AiDetected;
    
    [JsonIgnore]
    public bool IsCustomGame => UploadStatus == UploadStatus.CustomGame;
    
    [JsonIgnore]
    public bool IsPtrRegion => UploadStatus == UploadStatus.PtrRegion;
    
    [JsonIgnore]
    public bool IsIncomplete => UploadStatus == UploadStatus.Incomplete;
    
    [JsonIgnore]
    public bool IsTooOld => UploadStatus == UploadStatus.TooOld;

    public StormReplayInfo()
    {
        
    }
    
    [JsonConstructor]
    public StormReplayInfo(string filePath)
    {
        FilePath = filePath;
        Created = File.GetCreationTime(filePath);
    }           

    public override string ToString() => FilePath;

    public int CompareTo(StormReplayInfo? other)
    {
        if (other == null) {
            return 1;
        }

        return Created.CompareTo(other.Created);
    }

    public override int GetHashCode() => FilePath.GetHashCode() ^ Created.GetHashCode();
}
