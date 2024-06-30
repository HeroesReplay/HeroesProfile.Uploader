using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Services;

public interface IFileMonitor
{
    event EventHandler<EventArgs<string>> BattleLobbyCreated;
    event EventHandler<EventArgs<string>> StormSaveCreated;
    event EventHandler<EventArgs<string>> StormReplayCreated;

    public bool IsBattleLobbyEnabled { get; set; }
    public bool IsStormSaveEnabled { get; set; }

    IEnumerable<StormReplayInfo> GetAllStormReplayFiles();
}

public sealed class FileMonitor : IFileMonitor
{
    private readonly string _battleLobbyTempPath = Path.GetTempPath();

    private readonly string _stormSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");
    private readonly string _stormReplayPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");

    public IEnumerable<StormReplayInfo> GetAllStormReplayFiles()
    {
        return Directory
            .EnumerateFiles(_stormSavePath, "*.StormReplay", SearchOption.AllDirectories)
            .Select(x => new StormReplayInfo() { Created = File.GetCreationTime(x), FilePath = x });
    }

    public event EventHandler<EventArgs<string>>? BattleLobbyCreated;
    public event EventHandler<EventArgs<string>>? StormSaveCreated;
    public event EventHandler<EventArgs<string>>? StormReplayCreated;

    public bool IsBattleLobbyEnabled
    {
        get => _battlelobbyWatcher.EnableRaisingEvents;
        set => _battlelobbyWatcher.EnableRaisingEvents = value;
    }

    public bool IsStormReplayEnabled
    {
        get => _stormSaveWatcher.EnableRaisingEvents;
        set => _stormSaveWatcher.EnableRaisingEvents = value;
    }

    public bool IsStormSaveEnabled
    {
        get => _stormSaveWatcher.EnableRaisingEvents;
        set => _stormSaveWatcher.EnableRaisingEvents = value;
    }

    private readonly FileSystemWatcher _battlelobbyWatcher;
    private readonly FileSystemWatcher _stormSaveWatcher;
    private readonly FileSystemWatcher _stormReplayWatcher;
    private readonly ILogger<FileMonitor> _logger;


    public FileMonitor(ILogger<FileMonitor> logger)
    {
        _logger = logger;

        _battlelobbyWatcher = new FileSystemWatcher() { Path = _battleLobbyTempPath, Filter = "*.battlelobby", IncludeSubdirectories = true };
        _battlelobbyWatcher.Changed -= OnBattleLobbyAdded;
        _battlelobbyWatcher.Changed += OnBattleLobbyAdded;

        _stormSaveWatcher = new FileSystemWatcher() { Path = _stormSavePath, Filter = "*.StormSave", IncludeSubdirectories = true };
        _stormSaveWatcher.Created -= OnStormSaveAdded;
        _stormSaveWatcher.Created += OnStormSaveAdded;

        _stormReplayWatcher = new FileSystemWatcher() { Path = _stormReplayPath, Filter = "*.StormReplay", IncludeSubdirectories = true };
        _stormReplayWatcher.Created -= OnStormReplayAdded;
        _stormReplayWatcher.Created += OnStormReplayAdded;
    }

    private void OnStormReplayAdded(object sender, FileSystemEventArgs e)
    {
        using (_logger.BeginScope("GameMonitor.OnStormReplayAdded")) {
            _logger.LogDebug("Detected new storm replay: {FullPath}", e.FullPath);
            StormReplayCreated?.Invoke(this, new EventArgs<string>(e.FullPath));
        }
    }

    private void OnBattleLobbyAdded(object source, FileSystemEventArgs e)
    {
        using (_logger.BeginScope("GameMonitor.OnBattleLobbyAdded")) {
            _logger.LogDebug("Detected new temp live replay: {FullPath}", e.FullPath);
            BattleLobbyCreated?.Invoke(this, new EventArgs<string>(e.FullPath));
        }
    }

    private void OnStormSaveAdded(object source, FileSystemEventArgs e)
    {
        using (_logger.BeginScope("GameMonitor.OnStormSaveAdded")) {
            _logger.LogDebug("Detected new storm save: {FullPath}", e.FullPath);
            StormSaveCreated?.Invoke(this, new EventArgs<string>(e.FullPath));
        }
    }
}