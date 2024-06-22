using System.Collections.Generic;

namespace Heroesprofile.Uploader.Common
{
    public interface IReplayStorage
    {
        void Save(IEnumerable<ReplayFile> files);
        IEnumerable<ReplayFile> Load();
    }
}
