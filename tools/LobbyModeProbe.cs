#:project ../Heroes.ReplayParser/Heroes.ReplayParser/Heroes.ReplayParser.csproj
#:property RestorePackagesWithLockFile=false

// Looks for anything in a game's lobby file (replay.server.battlelobby) that gives away its game mode.
//
// Rank capture starts when the game writes the lobby file at the loading screen, but the game mode
// (replay.initData's m_ammId) only arrives with the first storm save, ~80 s into the match. If the lobby
// file itself tells Storm League apart from the other modes, capture can start only for ranked games.
// Every .StormReplay carries the same lobby file next to its initData, so a folder of old replays is a
// labelled sample set: this reads both from each replay and reports what separates the modes.
//
// Usage (Windows, from the repo root):
//   dotnet run tools/LobbyModeProbe.cs -- ["<folder with .StormReplay files>"] [max replays]
// The folder defaults to Documents\Heroes of the Storm\Accounts (searched recursively), newest first,
// up to 1500 replays. Nothing is written or sent anywhere; the report goes to the console.

using Foole.Mpq;
using Heroes.ReplayParser;
using Heroes.ReplayParser.MPQFiles;
using System.Text;

var folder = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Heroes of the Storm", "Accounts");
var max = args.Length > 1 && int.TryParse(args[1], out var m) ? m : 1500;

if (!Directory.Exists(folder)) {
    Console.Error.WriteLine($"No such folder: {folder}");
    return 1;
}

var files = new DirectoryInfo(folder)
    .EnumerateFiles("*.StormReplay", SearchOption.AllDirectories)
    .OrderByDescending(f => f.LastWriteTimeUtc)
    .Take(max)
    .ToList();
Console.WriteLine($"Reading {files.Count} replays from {folder}");

var options = new ParseOptions {
    IgnoreErrors = true,
    ShouldParseEvents = false,
    ShouldParseUnits = false,
    ShouldParseMouseEvents = false,
    ShouldParseDetailedBattleLobby = false,
    ShouldParseStatistics = false,
};

var samples = new List<Sample>();
var failed = 0;
foreach (var file in files) {
    try {
        var replay = DataParser.ParseReplay(file.FullName, false, options).Item2;
        if (replay == null || replay.GameMode == GameMode.Unknown) {
            failed++;
            continue;
        }
        byte[] lobby;
        using (var archive = new MpqArchive(file.FullName)) {
            archive.AddListfileFilenames();
            lobby = DataParser.GetMpqFile(archive, ReplayServerBattlelobby.FileName);
        }
        // What the uploader reads from the lobby file today; -1 where it can't (no region marker).
        var humans = -1;
        try {
            humans = StandaloneBattleLobbyParser.Parse(lobby).Players.Length;
        }
        catch (Exception) {
        }
        samples.Add(new Sample(file.Name, replay.GameMode, replay.ReplayBuild, lobby, humans));
    }
    catch (Exception) {
        failed++;
    }
}
Console.WriteLine($"Read {samples.Count}, skipped {failed} (unreadable or unknown mode)");
Console.WriteLine();

var byMode = samples.GroupBy(s => s.Mode).OrderByDescending(g => g.Count()).ToList();

Console.WriteLine("== Per mode: count, lobby size, human players found by the standalone parser, builds");
foreach (var g in byMode) {
    Console.WriteLine($"{g.Key,-14} {g.Count(),5} games   size {g.Min(s => s.Lobby.Length)}-{g.Max(s => s.Lobby.Length)} bytes   " +
        $"humans {g.Min(s => s.Humans)}-{g.Max(s => s.Humans)}   builds {g.Min(s => s.Build)}-{g.Max(s => s.Build)}");
}
Console.WriteLine();

// Attribute values are 4-char codes, stored either way round. These are the draft and lobby ones the
// parser knows about (ReplayAttributeEvents.cs): ban modes, draft/blind pick, ready modes, custom marker.
string[] markers = { "1ban", "2ban", "3ban", "Mban", "draf", "drft", "blnd", "fcfs", "stan", "pick", "CSTM", "Cust", "Amm" };
Console.WriteLine("== Known attribute codes: % of each mode's lobby files containing them (as written / reversed)");
Console.WriteLine($"{"code",-6} " + string.Join(" ", byMode.Select(g => $"{g.Key,14}")));
foreach (var marker in markers) {
    var forward = Encoding.ASCII.GetBytes(marker);
    var backward = forward.Reverse().ToArray();
    var cells = byMode.Select(g => {
        var f = Percent(g.Count(s => IndexOf(s.Lobby, forward) >= 0), g.Count());
        var b = Percent(g.Count(s => IndexOf(s.Lobby, backward) >= 0), g.Count());
        return $"{f,6}/{b,-7}";
    });
    Console.WriteLine($"{marker,-6} " + string.Join(" ", cells));
}
Console.WriteLine();

// Without knowing the format: any 4-byte sequence present in (nearly) every lobby file of one mode and
// (nearly) none of another mode's is a candidate marker. Shown for Storm League against each other mode.
const int MinGames = 5;
var stormLeague = samples.Where(s => s.Mode == GameMode.StormLeague).ToList();
if (stormLeague.Count < MinGames) {
    Console.WriteLine($"Only {stormLeague.Count} Storm League games - need at least {MinGames} for the byte comparison.");
    return 0;
}

var others = byMode.Where(g => g.Key != GameMode.StormLeague && g.Count() >= MinGames).ToList();
Console.WriteLine("== 4-byte sequences in >= 98% of Storm League lobby files, by how rare they are in other modes");
var slCounts = CountGrams(stormLeague);
var common = slCounts.Where(kv => kv.Value >= stormLeague.Count * 0.98).Select(kv => kv.Key).ToHashSet();
Console.WriteLine($"{common.Count} candidates");

var otherCounts = others.ToDictionary(g => g.Key, g => CountGrams(g.ToList(), common));
var ranked = common
    .Select(gram => new {
        Gram = gram,
        Worst = others.Count == 0 ? 0 : others.Max(g => (double)otherCounts[g.Key].GetValueOrDefault(gram) / g.Count()),
    })
    .OrderBy(x => x.Worst)
    .Take(40)
    .ToList();

Console.WriteLine($"{"bytes",-12} {"text",-6} {"SL",5} " + string.Join(" ", others.Select(g => $"{g.Key,14}")) + "  first offset in an SL file (from start / from end)");
foreach (var r in ranked) {
    var bytes = BitConverter.GetBytes(r.Gram);
    var sl = Percent(slCounts[r.Gram], stormLeague.Count);
    var cells = others.Select(g => $"{Percent(otherCounts[g.Key].GetValueOrDefault(r.Gram), g.Count()),14}");
    var first = stormLeague[0].Lobby;
    var at = IndexOf(first, bytes);
    Console.WriteLine($"{Convert.ToHexString(bytes),-12} {Printable(bytes),-6} {sl,5} " + string.Join(" ", cells) + $"  {at} / {(at < 0 ? -1 : first.Length - at)}");
}
Console.WriteLine();
Console.WriteLine("A row with SL near 100% and every other column near 0% is a Storm League marker worth decoding.");
Console.WriteLine("Note: this compares byte-aligned sequences only; a field packed at a bit offset won't show up here.");
return 0;

static Dictionary<uint, int> CountGrams(List<Sample> games, HashSet<uint>? only = null)
{
    var counts = new Dictionary<uint, int>();
    var seen = new HashSet<uint>();
    foreach (var game in games) {
        seen.Clear();
        var data = game.Lobby;
        for (var i = 0; i + 4 <= data.Length; i++) {
            var gram = BitConverter.ToUInt32(data, i);
            if ((only == null || only.Contains(gram)) && seen.Add(gram)) {
                counts[gram] = counts.GetValueOrDefault(gram) + 1;
            }
        }
    }
    return counts;
}

static int IndexOf(byte[] data, byte[] needle)
{
    return data.AsSpan().IndexOf(needle);
}

static string Percent(int count, int total)
{
    return total == 0 ? "-" : $"{100.0 * count / total:0}%";
}

static string Printable(byte[] bytes)
{
    return new string(bytes.Select(b => b >= 0x20 && b < 0x7F ? (char)b : '.').ToArray());
}

record Sample(string File, GameMode Mode, int Build, byte[] Lobby, int Humans);
