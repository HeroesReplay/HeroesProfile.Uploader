using HeroesProfile.Uploader.Core.Enums;

namespace HeroesProfile.Uploader.ViewModels;

public sealed class StormReplayProcessResult
{
    public int Count { get; set; }
    public UploadStatus UploadStatus { get; set; }
}