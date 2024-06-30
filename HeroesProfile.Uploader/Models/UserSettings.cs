using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace HeroesProfile.Uploader.Models;

public sealed class UserSettings : INotifyPropertyChanged
{
    private bool _isPostMatchEnabled;
    private bool _isPreMatchEnabled;
    private bool _isMinimizeToTrayEnabled;
    private bool _isLaunchOnStartEnabled;

    [JsonPropertyName("minimizeToTray")]
    public bool IsMinimizeToTrayEnabled
    {
        get => _isMinimizeToTrayEnabled;
        set => SetField(ref _isMinimizeToTrayEnabled, value);
    }

    [JsonPropertyName("preMatchPage")]
    public bool IsPreMatchEnabled
    {
        get => _isPreMatchEnabled;
        set => SetField(ref _isPreMatchEnabled, value);
    }

    [JsonPropertyName("postMatchPage")]
    public bool IsPostMatchEnabled
    {
        get => _isPostMatchEnabled;
        set => SetField(ref _isPostMatchEnabled, value);
    }

    [JsonPropertyName("launchOnStart")]
    public bool IsLaunchOnStartEnabled
    {
        get => _isLaunchOnStartEnabled;
        set => SetField(ref _isLaunchOnStartEnabled, value);
    }

    public UserSettings()
    {

    }

    [field: JsonIgnore]
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}