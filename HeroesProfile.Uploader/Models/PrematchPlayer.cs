using System.Collections.Generic;
using System.Linq;

namespace HeroesProfile.Uploader.Models;

public class PrematchPlayer
{
    public long BattleNetId { get; set; }
    public int BattleNetRegionId { get; set; } = 0;
    public int BattleNetSubId { get; set; } = 0;
    public string? BattleNetTId { get; set; } = null;
    public int BattleTag { get; set; } = 0;
    
    public required string Name { get; set; }

    // public static IEnumerable<HeroesProfilePlayer> GetPlayersFrom(Heroes.ReplayParser.Replay battleLobbyReplay)
    // {		
    //     return battleLobbyReplay.Players.Select(x => new HeroesProfilePlayer()
    //     {
    //         BattleNetId = x.BattleNetId,
    //         BattleNetRegionId = x.BattleNetRegionId,
    //         BattleNetSubId = x.BattleNetSubId,
    //         BattleNetTId = x.BattleNetTId,
    //         BattleTag = x.BattleTag
    //     });
    // }

    public static IEnumerable<PrematchPlayer> GetPlayersFrom(Heroes.StormReplayParser.StormReplayPregame replay)
    {
        return replay.StormPlayers.Select(x => new PrematchPlayer() {
            BattleNetId = x.ToonHandle!.Id,
            BattleNetRegionId = x.ToonHandle.Region,
            BattleNetSubId = 0,
            BattleNetTId = null,
            BattleTag = int.Parse(x.BattleTagName.Split('#').Last()),
            Name = x.BattleTagName.Split('#').First()
        });
    }
}