using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.TrackerEvent;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Services;

public interface IPreMatchProcessor
{
    Task OpenPreMatchPage(string path);
}

public class PreMatchProcessor(ILogger<PreMatchProcessor> logger, HttpClient httpClient) : IPreMatchProcessor
{
    public bool IsPreMatchEnabled { get; set; } = false;


    public async Task OpenPreMatchPage(string path)
    {
        var result = StormReplayPregame.Parse(path, new ParsePregameOptions() { AllowPTR = false });
        logger.LogInformation("Parsed prematch {BattleLobbyPath} with status {Status}", path, result.Status);

        if (result.Status == StormReplayPregameParseStatus.Success) {
            await TryOpenPage(result.ReplayBattleLobby);
        }
    }

    private async Task TryOpenPage(StormReplayPregame stormReplayPregame)
    {
        try {
            var players = PrematchPlayer.GetPlayersFrom(stormReplayPregame);
            var json = JsonSerializer.Serialize(players);
            var values = new Dictionary<string, string> { { "data", json } };

            HttpResponseMessage response;

            using (var content = new FormUrlEncodedContent(values)) {
                response = await httpClient.PostAsync($"PreMatch", content);
            }

            if (response.IsSuccessStatusCode) {
                var body = await response.Content.ReadAsStringAsync();

                if (int.TryParse(body, out var value)) {
                    var path = httpClient.BaseAddress + $"PreMatch/Results?prematchID={value}";

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
            } else {
                logger.LogError("Failed to submit PreMatch content. Status code: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Error processing PreMatch");
        }
    }
}