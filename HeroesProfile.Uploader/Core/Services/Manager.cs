using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Replay;
using HeroesProfile.Uploader.Core.Enums;
using HeroesProfile.Uploader.Extensions;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public class Manager : INotifyPropertyChanged
{
    public Dictionary<UploadStatus, int> Aggregates
    {
        get => _aggregates;
        private init => _aggregates = value;
    }

    public ObservableCollectionEx<StormReplayInfo> Files => _files;

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly TimeSpan _waitTime = TimeSpan.FromSeconds(3);

    private readonly ILogger<Manager> _logger;
    private readonly IReplayStorage _storage;
    private readonly IReplayUploader _replayUploader;
    private readonly IAnalyzer _analyzer;
    private readonly IMonitor _monitor;
    private readonly ILiveMonitor _liveMonitor;
    private readonly IPreMatchProcessor _preMatchProcessor;

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
    private readonly Dictionary<UploadStatus, int> _aggregates = new();
    private readonly ObservableCollectionEx<StormReplayInfo> _files = new();
    private bool _postMatchPage;
    private string _status;
    private DeleteFiles _deleteAfterUpload;

    public Manager(ILogger<Manager> logger, IReplayStorage storage, IPreMatchProcessor preMatchProcessor, ILiveMonitor liveMonitor,
        IReplayUploader replayUploader, IAnalyzer analyzer, IMonitor monitor)
    {
        _logger = logger;
        _storage = storage;
        _liveMonitor = liveMonitor;
        _replayUploader = replayUploader;
        _analyzer = analyzer;
        _monitor = monitor;
        _preMatchProcessor = preMatchProcessor;

        Files.ItemPropertyChanged += (_, __) => RefreshStatusAndAggregates();
        Files.CollectionChanged += (_, __) => RefreshStatusAndAggregates();
    }

    public async Task Start()
    {
        if (_initialized) return;
        _initialized = true;

        var replays = ScanReplays();
        Files.AddRange(replays);
        replays.Where(x => x.UploadStatus == UploadStatus.None).Do(_processingQueue.Push);


        _monitor.ReplayAdded -= OnMonitorOnReplayAdded;
        _monitor.ReplayAdded += OnMonitorOnReplayAdded;

        _monitor.Start();
        StartBattleLobbyWatcherEvent();

        _ = Task.Run(UploadLoop);
    }

    private async void OnMonitorOnReplayAdded(object? _, EventArgs<string> e)
    {
        await EnsureFileAvailable(e.Data);

        if (PreMatchPage) {
            _liveMonitor.StopBattleLobbyWatcher();
            _liveMonitor.StopStormSaveWatcher();
        }

        StormReplayInfo replay = new StormReplayInfo(e.Data);

        Files.Insert(0, replay);
        _processingQueue.Push(replay);
    }

    private void StartBattleLobbyWatcherEvent()
    {
        if (PreMatchPage) {
            _liveMonitor.TempBattleLobbyCreated += async (_, e) => {
                _liveMonitor.StopBattleLobbyWatcher();
                await EnsureFileAvailable(e.Data);
                var tmpPath = Path.GetTempFileName();
                await SafeCopy(e.Data, tmpPath, true);
                await _preMatchProcessor.StartProcessing(tmpPath);
            };

            _liveMonitor.StartBattleLobby();
        }
    }

    public void Stop()
    {
        _monitor.Stop();
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
                        await _replayUploader.Upload(stormReplay, stormReplayInfo);
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
        Status = Files.Any(x => x.UploadStatus == UploadStatus.InProgress) ? "Uploading..." : "Idle";

        Aggregates.Clear();

        foreach (var item in Files.GroupBy(x => x.UploadStatus).ToDictionary(x => x.Key, x => x.Count())) {
            Aggregates.Add(item.Key, item.Value);
        }

        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Aggregates)));
    }

    static UploadStatus[] _ignored = [UploadStatus.None, UploadStatus.UploadError, UploadStatus.InProgress];

    private void SaveReplayList()
    {
        try {
            _storage.Save(Files.Where(x => !_ignored.Contains(x.UploadStatus)));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to save replay list");
        }
    }

    private List<StormReplayInfo> ScanReplays()
    {
        var replays = new List<StormReplayInfo>(_storage.Load());
        var lookup = new HashSet<StormReplayInfo>(replays);
        var filesToAdd = _monitor.ScanReplays().Select(x => new StormReplayInfo(x)).Where(x => !lookup.Contains(x));
        replays.AddRange(filesToAdd);
        return replays.OrderByDescending(x => x.Created).ToList();
    }

    private void DeleteReplay(StormReplayInfo file)
    {
        try {
            _logger.LogInformation("Deleting replay {Filename}", file.FilePath);
            file.Deleted = true;
            File.Delete(file.FilePath);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to delete replay {Filename}", file.FilePath);
        }
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
            catch (IOException) {
                // File is still in use
                await Task.Delay(100);
            }
            catch {
                return;
            }
        }
    }

    private bool ShouldDelete(StormReplayInfo stormReplayInfo, StormReplay stormReplay)
    {
        return
            DeleteAfterUpload.HasFlag(DeleteFiles.Ptr) && stormReplayInfo.UploadStatus == UploadStatus.PtrRegion ||
            DeleteAfterUpload.HasFlag(DeleteFiles.Ai) && stormReplayInfo.UploadStatus == UploadStatus.AiDetected ||
            DeleteAfterUpload.HasFlag(DeleteFiles.Custom) && stormReplayInfo.UploadStatus == UploadStatus.CustomGame ||
            stormReplayInfo.UploadStatus == UploadStatus.Success && (
                DeleteAfterUpload.HasFlag(DeleteFiles.Brawl) && stormReplay.GameMode == StormGameMode.Brawl ||
                DeleteAfterUpload.HasFlag(DeleteFiles.QuickMatch) && stormReplay.GameMode == StormGameMode.QuickMatch ||
                DeleteAfterUpload.HasFlag(DeleteFiles.UnrankedDraft) && stormReplay.GameMode == StormGameMode.UnrankedDraft ||
                DeleteAfterUpload.HasFlag(DeleteFiles.HeroLeague) && stormReplay.GameMode == StormGameMode.HeroLeague ||
                DeleteAfterUpload.HasFlag(DeleteFiles.TeamLeague) && stormReplay.GameMode == StormGameMode.TeamLeague ||
                DeleteAfterUpload.HasFlag(DeleteFiles.StormLeague) && stormReplay.GameMode == StormGameMode.StormLeague
            );
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