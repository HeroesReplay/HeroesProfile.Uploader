using System;
using System.IO;

namespace HeroesProfile.Uploader.Models;

public sealed class AppSettings
{
    private static readonly string SettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeroesProfile");
    public DirectoryInfo HeroesProfileAppData { get; private set; } = new(SettingsDir);
    public required string HeroesProfileApiUrl { get; set; }
    public required string HeroesProfileWebUrl { get; set; }
    public required UserSettings DefaultUserSettings { get; set; }

    public void CreateAppDataIfNotExists()
    {
        if (!HeroesProfileAppData.Exists) {
            HeroesProfileAppData.Create();
        }
    }
}