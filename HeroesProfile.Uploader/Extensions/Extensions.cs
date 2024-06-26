using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Heroes.StormReplayParser;

namespace HeroesProfile.Uploader.Extensions;

public static class Extensions
{
    public static void Do<T>(this IEnumerable<T> items, Action<T> action)
    {
        foreach(var item in items)
        {
            action(item);
        }
    }
    
    public static string GetFingerprint(this StormReplay replay)
    {
        var stringBuilder = new StringBuilder();
        replay.StormPlayers.Select(p => p.BattleTagName).OrderBy(x => x).Do(x => stringBuilder.Append((string?)x));
        stringBuilder.Append(replay.RandomValue);
        var md5 = MD5.HashData(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
        var result = new Guid(md5);
        return result.ToString();
    }
}
