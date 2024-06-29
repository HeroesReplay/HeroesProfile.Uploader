using System;
using System.IO;
using Heroes.StormReplayParser;
using ReactiveUI;

namespace HeroesProfile.Uploader.Models;

public class StormReplayInfo : ReactiveObject, IEquatable<StormReplayInfo>
{
    public StormReplay? StormReplay { get; set; }
    public string? Fingerprint { get; set; }
    public string FileName => Path.GetFileNameWithoutExtension(FilePath);
    
    public bool IsSuccess => UploadStatus == UploadStatus.Success;
    public bool IsWarning => UploadStatus != UploadStatus.InProgress && UploadStatus != UploadStatus.Success && UploadStatus != UploadStatus.UploadError;
    public bool IsError => UploadStatus == UploadStatus.UploadError;
    
    public string FilePath { get; set; } = null!;
    public required DateTime Created { get; set; }
    
    public bool Deleted { get; set; } = false;

    UploadStatus _uploadStatus = UploadStatus.Pending;

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

    public StoredReplayInfo ToStorageReplay() => new() {
        FilePath = FilePath, 
        Created = File.GetCreationTime(FilePath), 
        UploadStatus = UploadStatus
    };

    public int CompareTo(StormReplayInfo? other)
    {
        if (other == null)
            return 1;

        return string.Compare(FilePath, other.FilePath, StringComparison.Ordinal);
    }

    public bool Equals(StormReplayInfo? other)
    {
        return other != null && FilePath.Equals(other.FilePath) && Created.Equals(other.Created);
    }

    public override string ToString() => FilePath;
}