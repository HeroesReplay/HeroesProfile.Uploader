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
using Polly;
using Polly.Retry;
using ReactiveUI;

namespace HeroesProfile.Uploader.Services;

public interface IManager
{
    ISourceCache<StormReplayInfo, string> Files { get; }

    bool IsPreMatchEnabled { get; set; }
    bool IsPostMatchEnabled { get; set; }

    Task StartAsync(CancellationToken token);
    void Stop();
}

public class Manager(
    ILogger<Manager> logger,
    IReplayStorer replayStorer,
    IPreMatchProcessor prematchProcessor,
    IFileMonitor fileMonitor,
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

    private Task? _processingTask;

    readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<IOException>()
        .WaitAndRetryAsync(
            retryCount: 10,
            sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100),
            onRetry: (exception, span, retry, ctx) => { logger.LogWarning(exception, "Error accessing file. Retry {Retry} in {Span}", retry, span); });

    private readonly ReplayFileComparer _comparer = new();

    public bool IsPreMatchEnabled
    {
        get => _preMatchPage;
        set {
            _preMatchPage = value;
            fileMonitor.IsBattleLobbyEnabled = value;
        }
    }

    public bool IsPostMatchEnabled
    {
        get => _postMatchPage;
        set {
            _postMatchPage = value;
            replayUploader.IsPostMatchEnabled = value;
        }
    }

    private CancellationToken _token;

    public async Task StartAsync(CancellationToken token)
    {
        if (_initialized) return;
        _initialized = true;

        var replays = await ScanReplaysAsync();
        Files.AddOrUpdate(replays);
        replays.Where(x => x.UploadStatus == UploadStatus.Pending).Do(_processingQueue.Push);

        fileMonitor.StormSaveCreated += OnStormSaveAdded;
        fileMonitor.BattleLobbyCreated += OnBattleLobbyCreated;
        fileMonitor.StormReplayCreated += OnStormReplayAdded;

        _token = token;
        _processingTask = Task.Factory.StartNew(Process, _token);
    }

    private void OnStormSaveAdded(object? sender, EventArgs<string> e)
    {
    }

    private async void OnStormReplayAdded(object? _, EventArgs<string> e)
    {
        await EnsureFileAvailable(e.Data);

        if (IsPreMatchEnabled) {
            fileMonitor.IsBattleLobbyEnabled = false;
            fileMonitor.IsStormSaveEnabled = false;
        }

        StormReplayInfo replay = new StormReplayInfo { FilePath = e.Data, Created = File.GetCreationTime(e.Data) };

        Files.AddOrUpdate(replay);
        _processingQueue.Push(replay);
    }

    private async void OnBattleLobbyCreated(object? _, EventArgs<string> e)
    {
        fileMonitor.IsBattleLobbyEnabled = false;

        try {
            EnsureFileAvailable(e.Data);
            var battleLobbyPath = Path.GetTempFileName();
            await CopyBattleLobbyToSafeLocation(source: e.Data, destination: battleLobbyPath, true);
            await prematchProcessor.OpenPreMatchPage(battleLobbyPath);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to process battle lobby for PreMatch");
        }


        fileMonitor.IsBattleLobbyEnabled = true;
    }

    public void Stop()
    {
        fileMonitor.IsBattleLobbyEnabled = false;
        fileMonitor.IsStormSaveEnabled = false;
        _processingQueue.Clear();
    }

    private async Task Process()
    {
        while (!_token.IsCancellationRequested) {
            logger.LogInformation("Manager loop...");

            while (_processingQueue.Any()) {
                logger.LogInformation("Processing queue");

                try {
                    if (_processingQueue.TryPop(out var stormReplayInfo)) {
                        stormReplayInfo.UploadStatus = UploadStatus.InProgress;

                        replayAnalyzer.SetAnalysis(stormReplayInfo);

                        if (stormReplayInfo is { UploadStatus: UploadStatus.InProgress, StormReplay: not null }) {
                            await replayUploader.UploadAsync(stormReplayInfo);
                        } else {
                            stormReplayInfo.UploadStatus = UploadStatus.Incomplete;
                        }

                        await SaveProcessedReplays();
                    }
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Failure in upload loop");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), _token);
        }
    }

    static readonly UploadStatus[] Ignored = [
        UploadStatus.Pending,
        UploadStatus.UploadError,
        UploadStatus.InProgress
    ];

    private async Task SaveProcessedReplays()
    {
        try {
            var itemsToStore = _files.Items
                .Where(x => !Ignored.Contains(x.UploadStatus))
                .Select(x => x.ToStorageReplay())
                .ToArray();

            await replayStorer.SaveAsync(itemsToStore);
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

        IEnumerable<StormReplayInfo> filesToAdd = fileMonitor.GetAllStormReplayFiles().Where(x => !lookup.Contains(x));
        replays.AddRange(filesToAdd);

        return replays.OrderByDescending(x => x.Created).ToList();
    }

    private async Task EnsureFileAvailable(string filename, bool testWrite = true)
    {
        var response = await _retryPolicy.ExecuteAndCaptureAsync((t) => {
            if (testWrite) {
                using (File.OpenWrite(filename)) {
                    logger.LogInformation("File {Filename} is available", filename);
                }
            } else {
                using (File.OpenRead(filename)) {
                    logger.LogInformation("File {Filename} is available", filename);
                }
            }

            return Task.CompletedTask;
        }, CancellationToken.None);

        if (response.Outcome == OutcomeType.Failure) {
            logger.LogError(response.FinalException, "Error ensuring file is available");
            throw response.FinalException;
        }

        if (response.Outcome == OutcomeType.Successful) {
            logger.LogInformation("File {Filename} is available", filename);
        }
    }

    private async Task CopyBattleLobbyToSafeLocation(string source, string destination, bool overwrite)
    {
        await _retryPolicy.ExecuteAsync((t) => {
            File.Copy(source, destination, overwrite);
            return Task.CompletedTask;
        }, CancellationToken.None);
    }
}