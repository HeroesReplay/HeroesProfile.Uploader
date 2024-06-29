using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HeroesProfile.Uploader.Models;

using ReactiveUI;

public sealed class StormReplayProcessResult : ReactiveObject
{
    public bool IsError => UploadStatus == UploadStatus.UploadError;
    public bool IsSuccess => UploadStatus == UploadStatus.Success;
    public bool IsWarning => UploadStatus != UploadStatus.InProgress && UploadStatus != UploadStatus.Success && UploadStatus != UploadStatus.UploadError;

    private int _count;
    public int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    private UploadStatus _uploadStatus;
    public UploadStatus UploadStatus
    {
        get => _uploadStatus;
        set => this.RaiseAndSetIfChanged(ref _uploadStatus, value);
    }

    public StormReplayProcessResult(UploadStatus uploadStatus, int count)
    {
        UploadStatus = uploadStatus;
        Count = count;
    }
}