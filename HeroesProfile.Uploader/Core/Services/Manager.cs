using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using HeroesProfile.Uploader.Core.Enums;
using HeroesProfile.Uploader.Extensions;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public class Manager : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly TimeSpan _waitTime = TimeSpan.FromSeconds(5);

    private readonly ILogger<Manager> _logger;
    private readonly IReplayTrackerStorage _trackerStorage;
    private readonly IReplayUploader _replayUploader;
    private readonly IAnalyzer _analyzer;
    private readonly IGameFileMonitor _gameFileMonitor;
    private readonly IPreMatchProcessor _preMatchProcessor;

    public SourceList<StormReplayInfo> Files { get; } = new();

    private bool _initialized;

    private bool _preMatchPage;

    public bool PreMatchPage
    {
        get => _preMatchPage;
        set {
            _preMatchPage = value;
            _preMatchProcessor.PreMatchPage = value;
        }
    }

    public bool PostMatchPage
    {
        get => _postMatchPage;
        set {
            _postMatchPage = value;
            _replayUploader.PostMatchPage = value;
        }
    }

    public string Status
    {
        get => _status;
        private set => _status = value;
    }


    public DeleteFiles DeleteAfterUpload
    {
        get => _deleteAfterUpload;
        set => _deleteAfterUpload = value;
    }

    private readonly ConcurrentStack<StormReplayInfo> _processingQueue = new();
    private bool _postMatchPage;
    private string _status;
    private DeleteFiles _deleteAfterUpload;

    public Manager(
        ILogger<Manager> logger,
        IReplayTrackerStorage trackerStorage,
        IPreMatchProcessor preMatchProcessor,
        IGameFileMonitor gameFileMonitor,
        IReplayUploader replayUploader,
        IAnalyzer analyzer)
    {
        _logger = logger;
        _trackerStorage = trackerStorage;
        _gameFileMonitor = gameFileMonitor;
        _replayUploader = replayUploader;
        _analyzer = analyzer;
        _preMatchProcessor = preMatchProcessor;
    }

    public async Task Start()
    {
        if (_initialized) return;
        _initialized = true;

        var replays = await ScanReplaysAsync();
        Files.AddRange(replays);
        replays.Where(x => x.UploadStatus == UploadStatus.None).Do(_processingQueue.Push);


        _gameFileMonitor.StormSaveCreated -= OnGameReplayFileMonitorOnReplayAdded;
        _gameFileMonitor.StormSaveCreated += OnGameReplayFileMonitorOnReplayAdded;

        _gameFileMonitor.StartStormSave();

        StartBattleLobbyWatcherEvent();

        _ = Task.Run(UploadLoop);
    }

    private async void OnGameReplayFileMonitorOnReplayAdded(object? _, EventArgs<string> e)
    {
        await EnsureFileAvailable(e.Data);

        if (PreMatchPage) {
            _gameFileMonitor.StopBattleLobbyWatcher();
            _gameFileMonitor.StopStormSaveWatcher();
        }

        StormReplayInfo replay = new StormReplayInfo { FilePath = e.Data, Created = File.GetCreationTime(e.Data) };

        Files.Insert(0, replay);
        _processingQueue.Push(replay);
    }

    private void StartBattleLobbyWatcherEvent()
    {
        if (PreMatchPage) {
            _gameFileMonitor.TempBattleLobbyCreated += async (_, e) => {
                _gameFileMonitor.StopBattleLobbyWatcher();
                await EnsureFileAvailable(e.Data);
                var tmpPath = Path.GetTempFileName();
                await SafeCopy(e.Data, tmpPath, true);
                await _preMatchProcessor.StartProcessing(tmpPath);
            };
            _gameFileMonitor.StartBattleLobby();
        }
    }

    public void Stop()
    {
        _gameFileMonitor.StopBattleLobbyWatcher();
        _gameFileMonitor.StopStormSaveWatcher();
        _processingQueue.Clear();
    }

    private async Task UploadLoop()
    {
        while (_processingQueue.Any()) {
            try {
                if (_processingQueue.TryPop(out var stormReplayInfo)) {
                    stormReplayInfo.UploadStatus = UploadStatus.InProgress;
                    var stormReplay = _analyzer.Analyze(stormReplayInfo);
                    if (stormReplayInfo.UploadStatus == UploadStatus.InProgress && stormReplay != null) {
                        await _replayUploader.UploadAsync(stormReplayInfo);
                    } else {
                        stormReplayInfo.UploadStatus = UploadStatus.Incomplete;
                    }

                    SaveReplayList();
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failure in upload loop");
            }
        }
    }

    private void RefreshStatusAndAggregates()
    {
        Status = Files.Items.Any(x => x.UploadStatus == UploadStatus.InProgress) ? "Uploading..." : "Idle";
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
    }

    static UploadStatus[] _ignored = [UploadStatus.None, UploadStatus.UploadError, UploadStatus.InProgress];

    private void SaveReplayList()
    {
        try {
            _trackerStorage.SaveAsync(Files.Items.Where(x => !_ignored.Contains(x.UploadStatus)));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to save replay list");
        }
    }

    private async Task<List<StormReplayInfo>> ScanReplaysAsync()
    {
        var replays = new List<StormReplayInfo>(await _trackerStorage.LoadAsync());
        var lookup = new HashSet<StormReplayInfo>(replays);
        var filesToAdd = _gameFileMonitor.GetStormReplays().Select(filePath => new StormReplayInfo(filePath)).Where(x => !lookup.Contains(x));
        replays.AddRange(filesToAdd);
        return replays.OrderByDescending(x => x.Created).ToList();
    }

    public async Task EnsureFileAvailable(string filename, bool testWrite = true)
    {
        var timer = Stopwatch.StartNew();

        while (timer.Elapsed < _waitTime) {
            try {
                if (testWrite) {
                    File.OpenWrite(filename).Close();
                } else {
                    File.OpenRead(filename).Close();
                }

                return;
            }
            catch (Exception) {
                // File is still in use
                await Task.Delay(100);
            }
        }
    }

    private static async Task SafeCopy(string source, string destination, bool overwrite)
    {
        var watchdog = 10;
        var retry = false;

        do {
            try {
                File.Copy(source, destination, overwrite);
                retry = false;
            }
            catch (Exception ex) {
                Debug.WriteLine($"Failed to copy ${source} to ${destination}. Counter at ${watchdog} CAUSED BY ${ex}");
                if (watchdog <= 0) {
                    throw;
                }

                retry = true;
            }

            await Task.Delay(1000);
        } while (watchdog-- > 0 && retry);
    }
}