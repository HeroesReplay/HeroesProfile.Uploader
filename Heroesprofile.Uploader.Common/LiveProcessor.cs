using Heroes.ReplayParser;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net;

//Live File Parsers
using MpqBattlelobby = Heroes.ReplayParser.MPQFiles.StandaloneBattleLobbyParser;
using MpqHeader = Heroes.ReplayParser.MPQFiles.MpqHeader;
using MpqDetails = Heroes.ReplayParser.MPQFiles.ReplayDetails;
using MpqAttributeEvents = Heroes.ReplayParser.MPQFiles.ReplayAttributeEvents;
using MpqInitData = Heroes.ReplayParser.MPQFiles.ReplayInitData;
using MpqTrackerEvents = Heroes.ReplayParser.MPQFiles.ReplayTrackerEvents;

namespace Heroesprofile.Uploader.Common
{
    public class LiveProcessor : ILiveProcessor
    {
        public bool PreMatchPage { get; set; }



        private static Logger _log = LogManager.GetCurrentClassLogger();
        HttpClient client = new HttpClient();



        // v1, served from the main site. See the note in Uploader.cs.
#if DEBUG
        private static readonly string heresprofileAPI = @"http://127.0.0.1:8000/api/external/v1/";
        private static readonly string heresprofile = @"http://127.0.0.1:8000/";

#else
        private static readonly string heresprofileAPI = @"https://www.heroesprofile.com/api/external/v1/";
        private static readonly string heresprofile = @"https://www.heroesprofile.com/";

#endif


        private static readonly string preMatchURI = @"PreMatch/Results/?prematchID=";

        private Dictionary<int, int> playerIDTalentIndexDictionary = new Dictionary<int, int>();
        private Dictionary<string, string> foundTalents = new Dictionary<string, string>();

        private Replay replayData;

        public LiveProcessor(bool PreMatchPage)
        {
            this.PreMatchPage = PreMatchPage;
        }

        public async Task StartProcessing(string battleLobbyPath)
        {
            _log.Debug($"Live processing started for '{battleLobbyPath}' (PreMatchPage={PreMatchPage})");

            if (!PreMatchPage) {
                _log.Debug("Prematch page is disabled in settings, skipping");
                return;
            }

            try {
                byte[] replayBytes = File.ReadAllBytes(battleLobbyPath);
                _log.Debug($"Read {replayBytes.Length} bytes of battlelobby data");
                replayData = MpqBattlelobby.Parse(replayBytes);
            }
            catch (Exception ex) {
                _log.Error(ex, $"Failed to read or parse battlelobby '{battleLobbyPath}'");
                return;
            }

            var playerCount = replayData?.Players?.Count(x => x != null) ?? 0;
            _log.Debug($"Parsed battlelobby, found {playerCount} players");

            if (playerCount == 0) {
                // usually means the game was still writing the file when the watcher fired
                _log.Warn("No players parsed out of the battlelobby, skipping prematch");
                return;
            }

            await runPreMatch(replayData);
        }


        /// <summary>
        /// Upload replay data to Heroes Profile and open up PreMatch page
        /// </summary>
        private async Task runPreMatch(Replay replayData)
        {
            var apiUrl = $"{heresprofileAPI}prematch";

            try {
                var payload = JsonConvert.SerializeObject(replayData.Players);
                var values = new Dictionary<string, string>
                {
                    { "data", payload },
                };

                var content = new FormUrlEncodedContent(values);

                _log.Debug($"Posting prematch data to {apiUrl} ({payload.Length} chars)");
                var timer = Stopwatch.StartNew();
                var response = await client.PostAsync(apiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();
                timer.Stop();

                _log.Debug($"Prematch responded HTTP {(int)response.StatusCode} {response.ReasonPhrase} in {timer.ElapsedMilliseconds}ms, body: {Describe(responseString)}");

                if (!Int32.TryParse(responseString?.Trim(), out int value)) {
                    _log.Error($"Integer value not returned for prematch replayID. HTTP {(int)response.StatusCode} from {apiUrl}, response string: {Describe(responseString)}");
                    return;
                }

                var pageUrl = $"{heresprofile}{preMatchURI}{value}";
                _log.Info($"Opening prematch page {pageUrl}");
                try {
                    Process.Start(pageUrl);
                }
                catch (Exception ex) {
                    _log.Error(ex, $"Failed to open prematch page {pageUrl}");
                }
                WebhookNotifier.Notify("prematch", pageUrl);
            } catch (Exception ex) {
                _log.Error(ex, $"Prematch failed ({apiUrl})");
            }
        }

        /// <summary>
        /// Render a response body for the log: an error page is worth seeing, but not all 40kb of it
        /// </summary>
        private static string Describe(string response)
        {
            if (response == null) {
                return "<null>";
            }
            if (response.Length == 0) {
                return "<empty>";
            }
            var oneLine = response.Replace("\r", " ").Replace("\n", " ").Trim();
            return oneLine.Length > 500 ? $"{oneLine.Substring(0, 500)}... ({response.Length} chars total)" : oneLine;
        }
    }
}
