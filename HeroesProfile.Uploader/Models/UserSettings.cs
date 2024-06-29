using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace HeroesProfile.Uploader.Models;

public sealed class UserSettings : INotifyPropertyChanged
{
    private bool _postMatchPage;
    private bool _preMatchPage;
    private bool _minimizeToTray;
    private bool _launchOnStart;

    // [JsonPropertyName("windowTop")]
    // public int WindowTop { get; set; }
    //
    // [JsonPropertyName("windowLeft")]
    // public int WindowLeft { get; set; }
    //
    // [JsonPropertyName("windowHeight")]
    // public int WindowHeight { get; set; }
    //
    // [JsonPropertyName("windowWidth")]
    // public int WindowWidth { get; set; }

    [JsonPropertyName("minimizeToTray")]
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetField(ref _minimizeToTray, value);
    }

    [JsonPropertyName("preMatchPage")]
    public bool PreMatchPage
    {
        get => _preMatchPage;
        set => SetField(ref _preMatchPage, value);
    }

    [JsonPropertyName("postMatchPage")]
    public bool PostMatchPage
    {
        get => _postMatchPage;
        set => SetField(ref _postMatchPage, value);
    }

    [JsonPropertyName("launchOnStart")]
    public bool LaunchOnStart
    {
        get => _launchOnStart;
        set => SetField(ref _launchOnStart, value);
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