using System;
using System.IO;

namespace HeroesProfile.Uploader.Models;

public sealed class AppSettings {
    
    public DirectoryInfo? HeroesProfileAppData { get; private set; }
    
    public required int WindowTop { get; set; }
    public required int WindowLeft { get; set; }
    public required bool MinimizeToTray { get; set; }
    public required int WindowHeight { get; set; }
    public required int WindowWidth { get; set; }
    public required UserSettings? DefaultUserSettings { get; set; }
    
    private readonly string _settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeroesProfile");
    

    public void PostConfigure()
    {
        HeroesProfileAppData = new DirectoryInfo(_settingsDir);
        HeroesProfileAppData.Create();
    }
}