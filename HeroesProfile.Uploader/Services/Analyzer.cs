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
    StormReplay? Analyze(StormReplayInfo file);
}

public class ReplayAnalyzer(ILogger<ReplayAnalyzer> logger) : IReplayAnalyzer
{
    public int MinimumBuild { get; set; }

    public StormReplay? Analyze(StormReplayInfo file)
    {
        try {
            StormReplayResult result = StormReplay.Parse(file.FilePath,
                new ParseOptions() { AllowPTR = false, ShouldParseMessageEvents = false, ShouldParseGameEvents = false, ShouldParseTrackerEvents = false });

            if (result.Status != StormReplayParseStatus.Success) {
                file.UploadStatus = UploadStatus.Incomplete;
                return null;
            }

            if (result.Exception != null) {
            } else {
                var status = GetPreStatus(result.Replay, result.Status);

                if (status.HasValue) {
                    file.UploadStatus = status.Value;
                }
            }

            file.Fingerprint = result.Replay.GetFingerprint();

            return result.Replay;
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to analyze replay file {Filename}", file.FilePath);
            return null;
        }
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
            // return UploadStatus.CustomGame;
            return null;
        }

        return null;
    }
}