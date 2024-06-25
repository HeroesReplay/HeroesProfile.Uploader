using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

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

public class LiveMonitor : ILiveMonitor
{
    public event EventHandler<EventArgs<string>> TempBattleLobbyCreated;
    public event EventHandler<EventArgs<string>> StormSaveCreated;


    protected readonly string BattleLobbyTempPath = Path.GetTempPath();
    protected readonly string StormSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");
    protected FileSystemWatcher? BattlelobbyWatcher;
    protected FileSystemWatcher? StormsaveWatcher;

    private ILogger<LiveMonitor> logger;

    protected virtual void OnBattleLobbyAdded(object source, FileSystemEventArgs e)
    {
        logger.LogDebug("Detected new temp live replay: {FullPath}", e.FullPath);
        TempBattleLobbyCreated?.Invoke(this, new EventArgs<string>(e.FullPath));
    }

    protected virtual void OnStormSaveAdded(object source, FileSystemEventArgs e)
    {
        logger.LogDebug("Detected new storm save: {FullPath}", e.FullPath);
        StormSaveCreated?.Invoke(this, new EventArgs<string>(e.FullPath));
    }

    public void StartBattleLobby()
    {
        if (BattlelobbyWatcher is null) {
            BattlelobbyWatcher = new FileSystemWatcher() { Path = BattleLobbyTempPath, Filter = "*.battlelobby", IncludeSubdirectories = true };
            BattlelobbyWatcher.Changed -= OnBattleLobbyAdded;
            BattlelobbyWatcher.Changed += OnBattleLobbyAdded;
        }

        BattlelobbyWatcher.EnableRaisingEvents = true;

        logger.LogDebug("Started watching for new battlelobby");
    }

    public void StartStormSave()
    {
        if (StormsaveWatcher is null) {
            StormsaveWatcher = new FileSystemWatcher() { Path = StormSavePath, Filter = "*.StormSave", IncludeSubdirectories = true };
            StormsaveWatcher.Created -= OnStormSaveAdded;
            StormsaveWatcher.Created += OnStormSaveAdded;
        }

        StormsaveWatcher.EnableRaisingEvents = true;

        logger.LogDebug($"Started watching for new storm save");
    }

    public void StopBattleLobbyWatcher()
    {
        if (BattlelobbyWatcher != null) {
            BattlelobbyWatcher.EnableRaisingEvents = false;
        }

        logger.LogDebug($"Stopped watching for new replays");
    }
    
    public void StopStormSaveWatcher()
    {
        if (StormsaveWatcher != null) {
            StormsaveWatcher.EnableRaisingEvents = false;
        }

        logger.LogDebug($"Stopped watching for new storm save files");
    }

    public bool IsBattleLobbyRunning()
    {
        return BattlelobbyWatcher is { EnableRaisingEvents: true };
    }

    public bool IsStormSaveRunning()
    {
        return StormsaveWatcher is { EnableRaisingEvents: true };
    }
}