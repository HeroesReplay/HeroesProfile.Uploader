using System.Collections.Generic;

namespace HeroesProfile.Uploader.Models;

public class StormReplayInfoComparer : IComparer<StormReplayInfo>, IEqualityComparer<StormReplayInfo>
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

    public bool Equals(StormReplayInfo? x, StormReplayInfo? y)
    {
        return x != null && y != null && x.FilePath.Equals(y.FilePath) && x.Created.Equals(y.Created);
    }

    public int GetHashCode(StormReplayInfo obj)
    {
        return obj.FilePath.GetHashCode() ^ obj.Created.GetHashCode();
    }
}