using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public interface IMonitor
{
    event EventHandler<EventArgs<string>> ReplayAdded;

    string[] ScanReplays();
    void Start();
    void Stop();
}


public class Monitor(ILogger<Monitor> logger) : IMonitor
{
    public event EventHandler<EventArgs<string>>? ReplayAdded;

    private readonly string _profilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");

    private FileSystemWatcher? _watcher;

    protected virtual void OnReplayAdded(object source, FileSystemEventArgs e)
    {
        logger.LogDebug("Detected new replay: {FullPath}", e.FullPath);
        ReplayAdded?.Invoke(this, new EventArgs<string>(e.FullPath));
    }

    public void Start()
    {
        if (_watcher is null) {
            _watcher = new FileSystemWatcher { Path = _profilePath, Filter = "*.StormReplay", IncludeSubdirectories = true };
            _watcher.Created -= OnReplayAdded;
            _watcher.Created += OnReplayAdded;
        }
        _watcher.EnableRaisingEvents = true;
        logger.LogDebug($"Started watching for new replays");
    }

    public void Stop()
    {
        if (_watcher is not null) {
            _watcher.EnableRaisingEvents = false;
        }
        logger.LogDebug($"Stopped watching for new replays");
    }

    public string[] ScanReplays()
    {
        return Directory.GetFiles(_profilePath, "*.StormReplay", SearchOption.AllDirectories);
    }
}