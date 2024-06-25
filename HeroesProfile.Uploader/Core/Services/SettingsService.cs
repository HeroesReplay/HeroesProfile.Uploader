﻿using System;
using System.IO;
using System.Text.Json;

namespace HeroesProfile.Uploader.Core.Services;

public class SettingsService<T> where T : class
{
    private readonly string _filePath;

    public SettingsService(string fileName)
    {
        _filePath = GetLocalFilePath(fileName);
    }

    private string GetLocalFilePath(string fileName)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, fileName);
    }

    public T? LoadSettings()
    {
        try {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(_filePath), JsonSerializerOptions.Default);
        }
        catch {
            return null;
        }
    }

    public void SaveSettings(T settings)
    {
        string json = JsonSerializer.Serialize(settings);
        File.WriteAllText(_filePath, json);
    }
}
