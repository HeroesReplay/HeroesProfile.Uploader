﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public interface IGameFileMonitor
{
    event EventHandler<EventArgs<string>> TempBattleLobbyCreated;
    event EventHandler<EventArgs<string>> StormSaveCreated;
    
    void StartBattleLobby();
    void StartStormSave();
    void StopBattleLobbyWatcher();
    void StopStormSaveWatcher();
    bool IsBattleLobbyRunning();
    bool IsStormSaveRunning();
    IEnumerable<string> GetStormReplays();
}

public sealed class GameFileMonitor : IGameFileMonitor
{
    private readonly string _battleLobbyTempPath = Path.GetTempPath();
    private readonly string _stormSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");
    
    public IEnumerable<string> GetStormReplays()
    {
        return Directory.EnumerateFiles(_stormSavePath, "*.StormReplay", SearchOption.AllDirectories);
    }
   

    public event EventHandler<EventArgs<string>>? TempBattleLobbyCreated;
    public event EventHandler<EventArgs<string>>? StormSaveCreated;

    private readonly FileSystemWatcher _battlelobbyWatcher;
    private readonly FileSystemWatcher _stormsaveWatcher;
    private readonly ILogger<GameFileMonitor> _logger;

    public GameFileMonitor(ILogger<GameFileMonitor> logger)
    {
        _logger = logger;
        
        _battlelobbyWatcher = new FileSystemWatcher() { Path = _battleLobbyTempPath, Filter = "*.battlelobby", IncludeSubdirectories = true };
        _battlelobbyWatcher.Changed -= OnBattleLobbyAdded;
        _battlelobbyWatcher.Changed += OnBattleLobbyAdded;
        
        _stormsaveWatcher = new FileSystemWatcher() { Path = _stormSavePath, Filter = "*.StormSave", IncludeSubdirectories = true };
        _stormsaveWatcher.Created -= OnStormSaveAdded;
        _stormsaveWatcher.Created += OnStormSaveAdded;
    }

    private void OnBattleLobbyAdded(object source, FileSystemEventArgs e)
    {
        _logger.LogDebug("Detected new temp live replay: {FullPath}", e.FullPath);
        TempBattleLobbyCreated?.Invoke(this, new EventArgs<string>(e.FullPath));
    }

    private void OnStormSaveAdded(object source, FileSystemEventArgs e)
    {
        _logger.LogDebug("Detected new storm save: {FullPath}", e.FullPath);
        StormSaveCreated?.Invoke(this, new EventArgs<string>(e.FullPath));
    }

    public void StartBattleLobby()
    {
        _battlelobbyWatcher.EnableRaisingEvents = true;
        _logger.LogDebug("Started watching for new battlelobby");
    }

    public void StartStormSave()
    {
        _stormsaveWatcher.EnableRaisingEvents = true;
        _logger.LogDebug($"Started watching for new storm save");
    }

    public void StopBattleLobbyWatcher()
    {
        _battlelobbyWatcher.EnableRaisingEvents = false;
        _logger.LogDebug($"Stopped watching for new replays");
    }

    public void StopStormSaveWatcher()
    {
        _stormsaveWatcher.EnableRaisingEvents = false;
        _logger.LogDebug($"Stopped watching for new storm save files");
    }

    public bool IsBattleLobbyRunning() => _battlelobbyWatcher is { EnableRaisingEvents: true };

    public bool IsStormSaveRunning() => _stormsaveWatcher is { EnableRaisingEvents: true };
}