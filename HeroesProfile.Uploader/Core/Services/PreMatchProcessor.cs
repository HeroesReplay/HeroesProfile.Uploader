using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Heroes.StormReplayParser;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public interface IPreMatchProcessor
{
    bool PreMatchPage { get; set; }
    Task StartProcessing(string battleLobbyPath);
}

public class PreMatchProcessor(ILogger<PreMatchProcessor> logger) : IPreMatchProcessor
{
    public bool PreMatchPage { get; set; } = false;

    private static readonly string Heresprofile = @"https://www.heroesprofile.com/";

    private static readonly string PreMatchUri = @"PreMatch/Results/?prematchID=";

    private Dictionary<int, int> _playerIdTalentIndexDictionary = new Dictionary<int, int>();
    private Dictionary<string, string> _foundTalents = new Dictionary<string, string>();


    public async Task StartProcessing(string battleLobbyPath)
    {
        if (PreMatchPage) {
            var result = StormReplayPregame.Parse(battleLobbyPath, new ParsePregameOptions() { AllowPTR = false });
            if (result.Status == StormReplayPregameParseStatus.Success) {
                await Process(result.ReplayBattleLobby);
            }
        }
    }

    private async Task Process(StormReplayPregame stormReplayPregame)
    {
        try {
            using (HttpClient client = new HttpClient()) {
                
                var values = new Dictionary<string, string> {
                    { "data", JsonSerializer.Serialize(stormReplayPregame.StormPlayers) },
                };

                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync($"{Heresprofile}PreMatch/", content);
                var body = await response.Content.ReadAsStringAsync();

                if (int.TryParse(body, out var value)) {
                    System.Diagnostics.Process.Start($"{Heresprofile}{PreMatchUri}{value}");
                } else {
                    logger.LogError("Integer value not returned for postmatch replayID. Response: {Body}", body);
                }
            }
        }
        catch(Exception e) {
            logger.LogError(e, "Error processing PreMatch");
        }
    }
}