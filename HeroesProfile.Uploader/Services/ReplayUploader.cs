using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Services;

public interface IReplayUploader
{
    bool IsPostMatchEnabled { get; set; }
    Task<UploadStatus> UploadAsync(StormReplayInfo stormReplayInfo);
}

public class ReplayUploader(ILogger<ReplayUploader> logger, HttpClient httpClient, IPostMatchProcessor postMatchProcessor) : IReplayUploader
{
    public bool IsPostMatchEnabled { get; set; }


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

    private async Task<StormReplayInfo[]> GetAlreadyUploaded(StormReplayInfo[] replays)
    {
        HashSet<string> fingerprints = new();

        foreach (var item in replays) {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(item.Fingerprint), nameof(replays));
            fingerprints.Add(item.Fingerprint!);
        }

        try {
            var payload = new StringContent(String.Join('\n', fingerprints));
            var response = await httpClient.PostAsync("/replays/fingerprints", payload);

            if (response.IsSuccessStatusCode) {
                var json = await response.Content.ReadAsStringAsync();
                string[] results = JsonDocument.Parse(json).RootElement.GetProperty("exists").EnumerateArray().Select(x => x.GetString()!).ToArray();
                return replays.Where(r => results.Contains(r.Fingerprint)).ToArray();
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
        HttpResponseMessage? response;

        using (MultipartFormDataContent content = new MultipartFormDataContent()) {
            content.Add(new StreamContent(File.OpenRead(stormReplayInfo.FilePath)), "file", stormReplayInfo.FileName);
            response = await httpClient.PostAsync($"upload/heroesprofile/desktop?fingerprint={stormReplayInfo.Fingerprint}&version={version}", content);
        }

        if (response.IsSuccessStatusCode) {
            try {
                UploadResult? uploadResult = await response.Content.ReadFromJsonAsync<UploadResult>();

                if (uploadResult is null) {
                    throw new Exception("Failed to parse UploadResult response");
                }

                stormReplayInfo.UploadStatus = uploadResult.Status;
                if (IsPostMatchEnabled) {
                    await postMatchProcessor.OpenPostMatchPage(stormReplayInfo, uploadResult);
                    return stormReplayInfo.UploadStatus;
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Error parsing upload response");
            }
        }

        logger.LogWarning("Error uploading file {FileName}: {Response}", filePath, response.StatusCode);
        return UploadStatus.UploadError;
    }
}