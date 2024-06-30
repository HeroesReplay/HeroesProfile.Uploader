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

public class ReplayStorer(ILogger<ReplayStorer> logger, AppSettings appSettings) : IReplayStorer, IDisposable
{
    private readonly string _filePath = Path.Combine(appSettings.HeroesProfileAppData.FullName, "replays.json");

    private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);

    private static JsonSerializerOptions _options = new() {
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter<UploadStatus>() }
    };

    public async Task<StoredReplayInfo[]> LoadAsync()
    {
        if (!File.Exists(_filePath)) {
            return [];
        }

        try {
            await SemaphoreSlim.WaitAsync();

            await using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                var items = JsonSerializer.Deserialize<StoredReplayInfo[]>(stream, _options);
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
                await JsonSerializer.SerializeAsync(stream, files, _options);
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