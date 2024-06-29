using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Heroes.StormReplayParser;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Services;

public interface IPreMatchProcessor
{
    bool PreMatchPage { get; set; }
    Task StartProcessing(string battleLobbyPath);
}

public class PreMatchProcessor(ILogger<PreMatchProcessor> logger, HttpClient httpClient) : IPreMatchProcessor
{
    public bool PreMatchPage { get; set; } = false;


    public async Task StartProcessing(string battleLobbyPath)
    {
        if (PreMatchPage) {
            var result = StormReplayPregame.Parse(battleLobbyPath, new ParsePregameOptions() { AllowPTR = false });

            logger.LogInformation("Parsed prematch {BattleLobbyPath} with status {Status}", battleLobbyPath, result.Status);

            if (result.Status == StormReplayPregameParseStatus.Success) {
                await PostPreMatch(result.ReplayBattleLobby);
            }
        }
    }

    private async Task PostPreMatch(StormReplayPregame stormReplayPregame)
    {
        try {
            var values = new Dictionary<string, string> { { "data", JsonSerializer.Serialize(stormReplayPregame.StormPlayers) }, };

            var content = new FormUrlEncodedContent(values);
            var response = await httpClient.PostAsync($"PreMatch/", content);
            var body = await response.Content.ReadAsStringAsync();

            if (int.TryParse(body, out var value)) {
                var path = httpClient.BaseAddress + $"PreMatch/Results/?prematchID={value}";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    Process.Start(path);
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", path);
                } else {
                    throw new NotSupportedException("Unsupported operating system");
                }
            } else {
                logger.LogError("Integer value not returned for postmatch replayID. Response: {Body}", body);
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Error processing PreMatch");
        }
    }
}