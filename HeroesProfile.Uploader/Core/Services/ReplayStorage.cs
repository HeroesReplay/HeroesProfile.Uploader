using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public interface IReplayStorage
{
    Task Save(IEnumerable<StormReplayInfo> files);
    StormReplayInfo[]? Load();
}

public class ReplayStorage(ILogger<ReplayStorage> logger) : IReplayStorage
{
    private static readonly string SettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Heroesprofile");
    private string _filePath = $@"{SettingsDir}\replays_v8.xml";
    
    private static readonly SemaphoreSlim SemaphoreSlim = new(0, 1);

    public StormReplayInfo[] Load()
    {
        if (!File.Exists(_filePath)) {
            return [];
        }

        try {
            using (var f = File.OpenRead(_filePath)) {
                var serializer = new XmlSerializer(typeof(StormReplayInfo[]));
                return (StormReplayInfo[])serializer.Deserialize(f)!;
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error loading replay upload data");
        }

        return [];
    }

    public async Task Save(IEnumerable<StormReplayInfo> files)
    {
        try {
            await SemaphoreSlim.WaitAsync();
            using (var stream = new MemoryStream()) {
                var data = files.ToArray();
                var serializer = new XmlSerializer(data.GetType());
                serializer.Serialize(stream, data);
                await File.WriteAllBytesAsync(_filePath, stream.ToArray(), default);
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error saving replay upload data");
        }
        finally {
            SemaphoreSlim.Release();
        }
    }
}