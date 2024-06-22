using System;

namespace Heroesprofile.Uploader.Common
{
    public interface ILiveMonitor
    {
        event EventHandler<EventArgs<string>> TempBattleLobbyCreated;
        event EventHandler<EventArgs<string>> StormSaveCreated;
        void StartBattleLobby();
        void StartStormSave();


        void StopBattleLobbyWatcher();
        void StopStormSaveWatcher();

        bool IsBattleLobbyRunning();
        bool IsStormSaveRunning();

    }
}