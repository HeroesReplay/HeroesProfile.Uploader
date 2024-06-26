using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HeroesProfile.Uploader.Core.Enums;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public interface IReplayUploader
{
    bool PostMatchPage { get; set; }

    Task<StormReplayInfo[]> GetAlreadyUploaded(StormReplayInfo[] replays);
    Task<UploadStatus> UploadAsync(StormReplayInfo stormReplayInfo);
}

public class ReplayUploader(ILogger<ReplayUploader> logger) : IReplayUploader
{
    public bool PostMatchPage { get; set; }

#if DEBUG
    const string HeroesProfileApiEndpoint = "http://127.0.0.1:8000/api";
    const string HeroesProfileMatchParsed = "http://127.0.0.1:8000/openApi/Replay/Parsed/?replayID=";
    const string HeroesProfileMatchSummary = "http://localhost/Match/Single/?replayID=";
#else
    const string HeroesProfileApiEndpoint = "https://api.heroesprofile.com/api";
    const string HeroesProfileMatchParsed = "https://api.heroesprofile.com/openApi/Replay/Parsed/?replayID=";
    const string HeroesProfileMatchSummary = "https://www.heroesprofile.com/Match/Single/?replayID=";
#endif

    public async Task<UploadStatus> UploadAsync(StormReplayInfo stormReplayInfo)
    {
        if (stormReplayInfo.Fingerprint is null)
            throw new ArgumentException("Fingerprint is null", nameof(stormReplayInfo.Fingerprint));

        stormReplayInfo.UploadStatus = UploadStatus.InProgress;

        var items = await GetAlreadyUploaded([stormReplayInfo]);

        if (items.Length > 0) {
            logger.LogInformation("File {StormReplayInfo} marked as duplicate", stormReplayInfo);
            stormReplayInfo.UploadStatus = UploadStatus.Duplicate;
        }

        if (stormReplayInfo.UploadStatus == UploadStatus.InProgress) {
            stormReplayInfo.UploadStatus = await PostAsync(stormReplayInfo);
        }

        return stormReplayInfo.UploadStatus;
    }

    public async Task<StormReplayInfo[]> GetAlreadyUploaded(StormReplayInfo[] replays)
    {
        HashSet<string> fingerprints = new();
        foreach (var item in replays) {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(item.Fingerprint), nameof(replays));
            fingerprints.Add(item.Fingerprint!);
        }

        try {
            using (var client = new HttpClient()) {
                var payload = new StringContent(String.Join('\n', fingerprints));
                var response = await client.PostAsync($"{HeroesProfileApiEndpoint}/replays/fingerprints", payload);

                if (response.IsSuccessStatusCode) {
                    var json = await response.Content.ReadAsStringAsync();
                    string[] results = JsonDocument.Parse(json).RootElement.GetProperty("exists").EnumerateArray().Select(x => x.GetString()!).ToArray();
                    return replays.Where(r => results.Contains(r.Fingerprint)).ToArray();
                }
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, $"Error checking fingerprint array");
        }

        return [];
    }

    private async Task<UploadStatus> PostAsync(StormReplayInfo stormReplayInfo)
    {
        var filePath = stormReplayInfo.FilePath;
        var version = "Avalonia";
        var result = UploadStatus.None;

        using (var client = new HttpClient()) {
            using (var content = new MultipartFormDataContent()) {
                content.Add(new StreamContent(File.OpenRead(stormReplayInfo.FilePath)), "file", stormReplayInfo.FileName);

                var requestUri = $"{HeroesProfileApiEndpoint}/upload/heroesprofile/desktop/?fingerprint={stormReplayInfo.Fingerprint}&version={version}";
                var response = await client.PostAsync(requestUri, content);

                if (response.IsSuccessStatusCode) {
                    var json = await response.Content.ReadAsStringAsync();
                    var uploadResult = UploadResult.FromJson(json);

                    if (uploadResult is null)
                        throw new Exception("Failed to parse UploadResult response");

                    if (!string.IsNullOrEmpty(uploadResult.Status)) {
                        if (Enum.TryParse(uploadResult.Status, out UploadStatus status)) {
                            logger.LogDebug("Uploaded file {FileName}: {Status}", filePath, status);
                            result = status;
                        } else {
                            logger.LogDebug("Unknown upload status {FileName}: {Status}", filePath, uploadResult.Status);
                            result = UploadStatus.UploadError;
                        }
                    }

                    await CheckPostMatch(stormReplayInfo, uploadResult);
                } else {
                    logger.LogWarning("Error uploading file {FileName}: {Response}", filePath, response.StatusCode);
                    return UploadStatus.UploadError;
                }
            }
        }
        
        return result;
    }

    private async Task CheckPostMatch(StormReplayInfo stormReplayInfo, UploadResult result)
    {
        try {
            if (PostMatchPage) {
                bool isValidPostMatchCondition =
                    result.ReplayId != 0 &&
                    File.GetLastWriteTime(stormReplayInfo.FilePath) >= DateTime.Now.Subtract(TimeSpan.FromMinutes(60));

                if (isValidPostMatchCondition) {
                    await PostMatchAnalysis(result.ReplayId);
                } else {
                    logger.LogWarning("Failed to open match page for replay {ReplayId} due to invalid condition", result.ReplayId);
                }
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to open match page");
        }
    }

    private async Task PostMatchAnalysis(int replayId)
    {
        using (var client = new HttpClient()) {
            var response = await client.GetAsync($"{HeroesProfileMatchParsed}{replayId}");

            var postMatchLink = $"{HeroesProfileMatchSummary}{replayId}";

            if (response.IsSuccessStatusCode) {
                var body = await response.Content.ReadAsStringAsync();

                if ("true".Equals(body, StringComparison.OrdinalIgnoreCase)) {
                   
                    if (OperatingSystem.IsMacOS()) {
                        Process.Start("open", postMatchLink);
                    } else if (OperatingSystem.IsWindows()) {
                        Process.Start(new ProcessStartInfo(postMatchLink) { UseShellExecute = true });
                    }
                }
            }
            
            logger.LogWarning("Failed to open match page for replay {ReplayId}", replayId);
        }
    }
}