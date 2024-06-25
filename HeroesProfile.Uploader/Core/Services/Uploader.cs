using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Heroes.StormReplayParser;
using HeroesProfile.Uploader.Core.Enums;
using HeroesProfile.Uploader.Extensions;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public interface IReplayUploader
{
    bool PostMatchPage { get; set; }
    Task CheckDuplicate(IEnumerable<StormReplayInfo> replayInfos);
    Task<UploadStatus> Upload(StormReplay stormReplay, StormReplayInfo stormReplayInfo, string? appVersion = null);
}

public class ReplayUploader(ILogger<ReplayUploader> logger) : IReplayUploader
{
#if DEBUG
    const string HeroesProfileApiEndpoint = "http://127.0.0.1:8000/api";
    const string HeroesProfileMatchParsed = "http://127.0.0.1:8000/openApi/Replay/Parsed/?replayID=";
    const string HeroesProfileMatchSummary = "http://localhost/Match/Single/?replayID=";
#else
    const string HeroesProfileApiEndpoint = "https://api.heroesprofile.com/api";
    const string HeroesProfileMatchParsed = "https://api.heroesprofile.com/openApi/Replay/Parsed/?replayID=";
    const string HeroesProfileMatchSummary = "https://www.heroesprofile.com/Match/Single/?replayID=";
#endif

    public async Task<UploadStatus> Upload(StormReplay stormReplay, StormReplayInfo stormReplayInfo)
    {
        stormReplayInfo.UploadStatus = UploadStatus.InProgress;

        if (await CheckDuplicate(stormReplayInfo.Fingerprint)) {
            logger.LogInformation("File {StormReplayInfo} marked as duplicate", stormReplayInfo);
            stormReplayInfo.UploadStatus = UploadStatus.Duplicate;
        }

        if (stormReplayInfo.UploadStatus == UploadStatus.InProgress) {
            stormReplayInfo.UploadStatus = await PostAsync(stormReplay, stormReplayInfo);
        }

        return stormReplayInfo.UploadStatus;
    }

    private async Task<UploadStatus> PostAsync(StormReplay stormReplay, StormReplayInfo stormReplayInfo)
    {
        var filePath = stormReplayInfo.FilePath;
        var version = "Avalonia"; // TODO: FIx

        using (var client = new HttpClient()) {
            using (var content = new MultipartFormDataContent()) {
                content.Add(new StreamContent(File.OpenRead(stormReplayInfo.FilePath)), "file", stormReplayInfo.FileName);
                var response = await client.PostAsync($"{HeroesProfileApiEndpoint}/upload/heroesprofile/desktop/?fingerprint={stormReplayInfo.Fingerprint}&version={version}", content);

                if (response.StatusCode != HttpStatusCode.OK) {
                    return UploadStatus.UploadError;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                UploadResult? result = UploadResult.FromJson(responseContent);

                int replayId = result.ReplayId;

                try {
                    if (PostMatchPage && File.GetLastWriteTime(stormReplayInfo.FilePath) >= DateTime.Now.Subtract(TimeSpan.FromMinutes(60)) && replayId != 0) {
                        await PostMatchAnalysis(replayId);
                    }
                }
                catch (Exception e) {
                    logger.LogError(e, "Failed to open match page");
                }

                if (!string.IsNullOrEmpty(result.Status)) {
                    if (Enum.TryParse(result.Status, out UploadStatus status)) {
                        logger.LogDebug("Uploaded file {FileName}: {Status}", filePath, status);
                        return status;
                    }

                    logger.LogDebug("Unknown upload status {FileName}: {Status}", filePath, result.Status);
                    return UploadStatus.UploadError;
                }

                logger.LogWarning("Error uploading file {FileName}: {Response}", filePath, responseContent);
                return UploadStatus.UploadError;
            }
        }
    }

    private async Task PostMatchAnalysis(int replayId)
    {
        var timer = Stopwatch.StartNew();

        using (var client = new HttpClient()) {
            while (timer.Elapsed < TimeSpan.FromSeconds(15)) {
                var response = await client.GetAsync($"{HeroesProfileMatchParsed}{replayId}");

                if (response.IsSuccessStatusCode) {
                    var body = await response.Content.ReadAsStringAsync();
                    if ("true".Equals(body, StringComparison.OrdinalIgnoreCase)) {
                        Process.Start($"{HeroesProfileMatchSummary}{replayId}");
                        return;
                    }
                } else {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }

            logger.LogWarning("Failed to open match page for replay {ReplayId}", replayId);
        }

        timer.Stop();
    }

    private async Task<bool> CheckDuplicate(string fingerprint)
    {
        try {
            using (var client = new HttpClient()) {
                var response = await client.GetAsync($"{HeroesProfileApiEndpoint}/replays/fingerprints/{fingerprint}");

                if (response.IsSuccessStatusCode) {
                    return (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.GetProperty("exists").GetBoolean();
                }

                if (await CheckApiThrottling(response)) {
                    return await CheckDuplicate(fingerprint);
                }
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to check duplicate");
        }

        return false;
    }

    /// <summary>
    /// Mass check replay fingerprints against database to detect duplicates
    /// </summary>
    /// <param name="fingerprints"></param>
    private async Task<string[]> CheckDuplicate(IEnumerable<string> fingerprints)
    {
        try {
            using (var client = new HttpClient()) {
                HttpResponseMessage response = await client.PostAsync($"{HeroesProfileApiEndpoint}/replays/fingerprints",
                    new StringContent(String.Join("\n", fingerprints)));
                if (response.IsSuccessStatusCode) {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonDocument.Parse(json).RootElement.GetProperty("exists").EnumerateArray().Select(x => x.GetString()).ToArray();
                }

                if (await CheckApiThrottling(response)) {
                    return await CheckDuplicate(fingerprints);
                }
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, $"Error checking fingerprint array");
        }

        return Array.Empty<string>();
    }

    public bool PostMatchPage { get; set; }

    /// <summary>
    /// Mass check replay fingerprints against database to detect duplicates
    /// </summary>
    public async Task CheckDuplicate(IEnumerable<StormReplayInfo> replays)
    {
        var exists = new HashSet<string>(await CheckDuplicate(replays.Select(x => x.Fingerprint)));
        replays.Where(x => exists.Contains(x.Fingerprint)).Do(x => x.UploadStatus = UploadStatus.Duplicate);
    }

    public Task<UploadStatus> Upload(StormReplay stormReplay, StormReplayInfo stormReplayInfo, string? appVersion = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Check if Heroes Profile API request limit is reached and wait if it is
    /// </summary>
    /// <param name="response">Server response to examine</param>
    private async Task<bool> CheckApiThrottling(HttpResponseMessage response)
    {
        var tooManyRequests = response.StatusCode == HttpStatusCode.TooManyRequests;

        if (tooManyRequests) {
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        return tooManyRequests;
    }
}