using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HeroesProfile.Uploader.Services;


public interface IPostMatchProcessor
{
    Task OpenPostMatchPage(StormReplayInfo stormReplayInfo, UploadResult result);
}

public class PostMatchProcessor(ILogger<PostMatchProcessor> logger, AppSettings appSettings, HttpClient httpClient) : IPostMatchProcessor
{
    
    public async Task OpenPostMatchPage(StormReplayInfo stormReplayInfo, UploadResult result)
    {
        try {
            bool isValidPostMatchCondition = result.ReplayId != 0 &&
                                             File.GetLastWriteTime(stormReplayInfo.FilePath) >= DateTime.Now.Subtract(TimeSpan.FromMinutes(60));

            if (isValidPostMatchCondition) {
                logger.LogInformation("Opening match page for replay {ReplayId}", result.ReplayId);
                await PostMatchAnalysis(result.ReplayId);
            } else {
                logger.LogWarning("Failed to open match page for replay {ReplayId} due to invalid condition", result.ReplayId);
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to open match page");
        }
    }

    private async Task PostMatchAnalysis(int replayId)
    {
        var response = await httpClient.GetAsync($"openApi/Replay/Parsed/?replayID={replayId}");

        if (response.IsSuccessStatusCode) {
            var body = await response.Content.ReadAsStringAsync();

            if ("true".Equals(body, StringComparison.OrdinalIgnoreCase)) {
                var postMatchLink = $"{appSettings.HeroesProfileWebUrl}/Match/Single/?replayID={replayId}";

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