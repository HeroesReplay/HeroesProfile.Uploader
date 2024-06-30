using System;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Replay;
using HeroesProfile.Uploader.Extensions;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Services;

public interface IReplayAnalyzer
{
    int MinimumBuild { get; set; }
    void SetAnalysis(StormReplayInfo file);
}

public class ReplayAnalyzer(ILogger<ReplayAnalyzer> logger) : IReplayAnalyzer
{
    public int MinimumBuild { get; set; }

    private static readonly ParseOptions Options = new() {
        AllowPTR = false, ShouldParseMessageEvents = false, ShouldParseGameEvents = false, ShouldParseTrackerEvents = false
    };

    public void SetAnalysis(StormReplayInfo file)
    {
        logger.LogInformation("Analyzing replay file {Filename}", file.FilePath);

        try {
            StormReplayResult result = StormReplay.Parse(file.FilePath, Options);

            if (result.Status != StormReplayParseStatus.Success) {
                file.UploadStatus = UploadStatus.Incomplete;
                file.StormReplay = null;
                return;
            }

            if (result.Exception != null) {
                logger.LogError(result.Exception, "Failed to analyze replay file {Filename}", file.FilePath);
                file.StormReplay = null;
                return;
            }

            var status = GetPreStatus(result.Replay, result.Status);

            logger.LogInformation("Parsed replay {Filename} with PreStatus {Status}", file.FilePath, status);

            if (status.HasValue) {
                file.UploadStatus = status.Value;
            }

            file.Fingerprint = result.Replay.GetFingerprint();
            file.StormReplay = result.Replay;
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to analyze replay file {Filename}", file.FilePath);
        }

        logger.LogInformation("Finished analyzing replay file {Filename}", file.FilePath);
    }

    private UploadStatus? GetPreStatus(StormReplay replay, StormReplayParseStatus parseResult)
    {
        switch (parseResult) {
            case StormReplayParseStatus.Incomplete:
                return UploadStatus.Incomplete;
            case StormReplayParseStatus.TryMeMode:
                return UploadStatus.AiDetected;
            case StormReplayParseStatus.PTRRegion:
                return UploadStatus.PtrRegion;
            case StormReplayParseStatus.PreAlphaWipe:
                return UploadStatus.TooOld;
        }

        if (parseResult != StormReplayParseStatus.Success) {
            return null;
        }

        if (replay.HasAI) {
            return UploadStatus.AiDetected;
        }

        if (replay.ReplayBuild < MinimumBuild) {
            return UploadStatus.TooOld;
        }

        if (replay.GameMode == StormGameMode.Custom) {
            return UploadStatus.CustomGame;
        }

        return null;
    }
}