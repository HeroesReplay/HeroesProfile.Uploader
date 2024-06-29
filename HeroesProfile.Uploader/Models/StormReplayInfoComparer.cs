using System.Collections.Generic;

namespace HeroesProfile.Uploader.Models;

public class StormReplayInfoComparer : IComparer<StormReplayInfo>
{
    public int Compare(StormReplayInfo? x, StormReplayInfo? y)
    {
        if (x == null || y == null) {
            return 0;
        }

        if (x.UploadStatus == UploadStatus.InProgress && y.UploadStatus != UploadStatus.InProgress) {
            return -1;
        }

        if (x.UploadStatus != UploadStatus.InProgress && y.UploadStatus == UploadStatus.InProgress) {
            return 1;
        }

        if (x.UploadStatus != y.UploadStatus) {
            return x.UploadStatus.CompareTo(y.UploadStatus);
        }

        return x.Created.CompareTo(y.Created);
    }
}