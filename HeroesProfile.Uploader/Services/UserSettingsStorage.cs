using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HeroesProfile.Uploader.Models;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader.Services;

public class UserSettingsStorage(ILogger<UserSettingsStorage> logger, AppSettings appSettings)
{
    private UserSettings? _userSettings;
    public UserSettings? UserSettings => _userSettings;

    private readonly string _filePath = Path.Combine(appSettings.HeroesProfileAppData.FullName, "user-settings.json");

    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath)) {
            _userSettings = appSettings.DefaultUserSettings;
            logger.LogInformation("User settings file not found, using default settings");
            return;
        }

        try {
            await using (var stream = File.OpenRead(_filePath)) {
                _userSettings = await JsonSerializer.DeserializeAsync<UserSettings>(stream, JsonSerializerOptions.Default);
                logger.LogInformation("Loaded user settings");
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Error loading user settings");
        }
    }

    public async Task SaveAsync()
    {
        await using (var stream = File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
            await JsonSerializer.SerializeAsync(stream, _userSettings);
            logger.LogInformation("Saved user settings");
        }
    }
}