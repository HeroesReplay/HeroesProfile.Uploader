using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Core.Services;

public interface IReplayTrackerStorage
{
    Task SaveAsync(IEnumerable<StormReplayInfo> files);
    Task<StormReplayInfo[]> LoadAsync();
}

public interface IStormReplayStorage
{
 
}

public class ReplayTrackerStorage(ILogger<ReplayTrackerStorage> logger) : IReplayTrackerStorage
{
    private static readonly string SettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeroesProfile");
    private readonly string _filePath = $@"{SettingsDir}\replays.json";
    private static readonly SemaphoreSlim SemaphoreSlim = new(0, 1);

    public async Task<StormReplayInfo[]> LoadAsync()
    {
        if (!File.Exists(_filePath)) {
            return [];
        }

        try {
            await SemaphoreSlim.WaitAsync();
            await using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                var items = JsonSerializer.Deserialize<StormReplayInfo[]>(stream);
                return items ?? [];
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error loading replay upload data");
        }
        finally {
            SemaphoreSlim.Release();
        }

        return [];
    }

    public async Task SaveAsync(IEnumerable<StormReplayInfo> files)
    {
        try {
            await SemaphoreSlim.WaitAsync();
            var bytes = JsonSerializer.SerializeToUtf8Bytes(files);
            await File.WriteAllBytesAsync(_filePath, bytes, default);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error saving replay upload data");
        }
        finally {
            SemaphoreSlim.Release();
        }
    }
}