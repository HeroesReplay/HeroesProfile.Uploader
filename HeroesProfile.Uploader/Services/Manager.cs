using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using HeroesProfile.Uploader.Extensions;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace HeroesProfile.Uploader.Services;

public interface IManager
{
    ISourceCache<StormReplayInfo, string> Files { get; }

    bool PreMatchPage { get; set; }
    bool PostMatchPage { get; set; }

    Task StartAsync(CancellationToken token);
    void Stop();
}

public class Manager(
    ILogger<Manager> logger,
    IReplayStorer replayStorer,
    IPreMatchProcessor preMatchProcessor,
    IGameMonitor gameMonitor,
    IReplayUploader replayUploader,
    IReplayAnalyzer replayAnalyzer) : ReactiveObject, IManager
{
    public ISourceCache<StormReplayInfo, string> Files => _files;

    private readonly ISourceCache<StormReplayInfo, string> _files = new SourceCache<StormReplayInfo, string>(x => x.FileName);
    private readonly TimeSpan _waitTime = TimeSpan.FromSeconds(5);
    private readonly ConcurrentStack<StormReplayInfo> _processingQueue = new();

    private bool _initialized;
    private bool _preMatchPage;
    private bool _postMatchPage;

    private readonly StormReplayInfoComparer _comparer = new();

    public bool PreMatchPage
    {
        get => _preMatchPage;
        set {
            _preMatchPage = value;
            preMatchProcessor.PreMatchPage = value;
        }
    }

    public bool PostMatchPage
    {
        get => _postMatchPage;
        set {
            _postMatchPage = value;
            replayUploader.PostMatchPage = value;
        }
    }

    public async Task StartAsync(CancellationToken token)
    {
        if (_initialized) return;
        _initialized = true;

        var replays = await ScanReplaysAsync();
        Files.AddOrUpdate(replays);
        replays.Where(x => x.UploadStatus == UploadStatus.Pending).Do(_processingQueue.Push);

        gameMonitor.StormSaveCreated -= OnGameReplayMonitorOnReplayAdded;
        gameMonitor.StormSaveCreated += OnGameReplayMonitorOnReplayAdded;

        gameMonitor.StartStormSave();

        StartBattleLobbyWatcherEvent();

        _ = Task.Run(() => UploadLoop(token), token);
    }

    private async void OnGameReplayMonitorOnReplayAdded(object? _, EventArgs<string> e)
    {
        await EnsureFileAvailable(e.Data);

        if (PreMatchPage) {
            gameMonitor.StopBattleLobbyWatcher();
            gameMonitor.StopStormSaveWatcher();
        }

        StormReplayInfo replay = new StormReplayInfo { FilePath = e.Data, Created = File.GetCreationTime(e.Data) };

        Files.AddOrUpdate(replay);
        _processingQueue.Push(replay);
    }

    private void StartBattleLobbyWatcherEvent()
    {
        if (PreMatchPage) {
            gameMonitor.TempBattleLobbyCreated += async (_, e) => {
                gameMonitor.StopBattleLobbyWatcher();

                await EnsureFileAvailable(e.Data);
                var tmpPath = Path.GetTempFileName();
                await SafeCopy(e.Data, tmpPath, true);

                await preMatchProcessor.StartProcessing(tmpPath);
            };

            gameMonitor.StartBattleLobby();
        }
    }

    public void Stop()
    {
        gameMonitor.StopBattleLobbyWatcher();
        gameMonitor.StopStormSaveWatcher();
        _processingQueue.Clear();
    }

    private async Task UploadLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested) {
            logger.LogInformation("Manager loop...");

            while (_processingQueue.Any()) {
                logger.LogInformation("Processing queue");

                try {
                    if (_processingQueue.TryPop(out var stormReplayInfo)) {
                        stormReplayInfo.UploadStatus = UploadStatus.InProgress;
                        var stormReplay = replayAnalyzer.Analyze(stormReplayInfo);
                        if (stormReplayInfo.UploadStatus == UploadStatus.InProgress && stormReplay != null) {
                            await replayUploader.UploadAsync(stormReplayInfo);
                        } else {
                            stormReplayInfo.UploadStatus = UploadStatus.Incomplete;
                        }

                        SaveReplayList();
                    }
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Failure in upload loop");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), token);
        }
    }

    static readonly UploadStatus[] Ignored = [
        UploadStatus.Pending,
        UploadStatus.UploadError,
        UploadStatus.InProgress
    ];

    private void SaveReplayList()
    {
        try {
            var itemsToStore = _files.Items
                .Where(x => !Ignored.Contains(x.UploadStatus))
                .Select(x => x.ToStorageReplay())
                .ToArray();

            replayStorer.SaveAsync(itemsToStore);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to save replay list");
        }
    }

    private async Task<List<StormReplayInfo>> ScanReplaysAsync()
    {
        StoredReplayInfo[] storedReplays = await replayStorer.LoadAsync();
        List<StormReplayInfo> replays = new List<StormReplayInfo>(storedReplays.Select(sr => sr.ToStormReplayInfo()));

        HashSet<StormReplayInfo> lookup = new HashSet<StormReplayInfo>(replays, _comparer);

        var filesToAdd = gameMonitor.GetStormReplays()
            .Select(filePath => new StormReplayInfo() { Created = File.GetCreationTime(filePath), FilePath = filePath, })
            .Where(x => !lookup.Contains(x));

        replays.AddRange(filesToAdd);

        return replays.OrderByDescending(x => x.Created).ToList();
    }

    private async Task EnsureFileAvailable(string filename, bool testWrite = true)
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