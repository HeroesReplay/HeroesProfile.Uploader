using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HeroesProfile.Uploader.Services;

public interface IReplayStorer
{
    Task SaveAsync(StoredReplayInfo[] files);
    Task<StoredReplayInfo[]> LoadAsync();
}

public class ReplayStorer(ILogger<ReplayStorer> logger, IOptions<AppSettings> settings) : IReplayStorer, IDisposable
{
    private readonly string _filePath = Path.Combine(settings.Value.HeroesProfileAppData.FullName, "replays.json");

    private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);

    public async Task<StoredReplayInfo[]> LoadAsync()
    {
        if (!File.Exists(_filePath)) {
            return [];
        }

        try {
            await SemaphoreSlim.WaitAsync();

            await using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                var items = JsonSerializer.Deserialize<StoredReplayInfo[]>(stream);
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

    public async Task SaveAsync(StoredReplayInfo[] files)
    {
        try {
            await SemaphoreSlim.WaitAsync();

            await using (var stream = File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
                await JsonSerializer.SerializeAsync(stream, files, new JsonSerializerOptions() {
                    WriteIndented = true,
                    Converters = {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                });
            }

            logger.LogInformation("Replay upload data saved");
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error saving replay upload data");
        }
        finally {
            SemaphoreSlim.Release();
        }
    }

    public void Dispose()
    {
        try {
            SemaphoreSlim.Dispose();
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error disposing semaphore");
        }
    }
}