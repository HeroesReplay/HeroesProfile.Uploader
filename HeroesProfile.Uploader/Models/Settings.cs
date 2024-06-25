using System.ComponentModel;
using System.Text.Json.Serialization;
using HeroesProfile.Uploader.Core.Enums;

namespace HeroesProfile.Uploader.Models;

public class UserSettings : INotifyPropertyChanged
{
    private int _windowTop;

    [JsonPropertyName("windowTop")]
    public int WindowTop 
    {
        get => _windowTop;
        set {
            _windowTop = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTop)));
        }
    }

    private int _windowLeft;

    [JsonPropertyName("windowLeft")]
    public int WindowLeft
    {
        get => _windowLeft;
        set {
            _windowLeft = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowLeft)));
        }
    }

    private bool _minimizeToTray;

    [JsonPropertyName("minimizeToTray")]
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set {
            _minimizeToTray = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinimizeToTray)));
        }
    }


    private int _windowHeight;

    [JsonPropertyName("windowHeight")]
    public int WindowHeight
    {
        get => _windowHeight;
        set {
            _windowHeight = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowHeight)));
        }
    }

    private int _windowWidth;

    [JsonPropertyName("windowWidth")]
    public int WindowWidth
    {
        get => _windowWidth;
        set {
            _windowWidth = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowWidth)));
        }
    }


    private bool _preMatchPage;

    [JsonPropertyName("preMatchPage")]

    public bool PreMatchPage
    {
        get => _preMatchPage;
        set {
            _preMatchPage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreMatchPage)));
        }
    }


    private bool _postMatchPage;

    [JsonPropertyName("postMatchPage")]

    public bool PostMatchPage
    {
        get => _postMatchPage;
        set {
            _postMatchPage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PostMatchPage)));
        }
    }

    public UserSettings()
    {

    }

    public UserSettings(AppSettings settings)
    {
        WindowHeight = settings.WindowHeight;
        WindowWidth = settings.WindowWidth;
        WindowTop = settings.WindowTop;
        WindowLeft = settings.WindowLeft;
        MinimizeToTray = settings.MinimizeToTray;
        PreMatchPage = settings.PreMatchPage;
        PostMatchPage = settings.PostMatchPage;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}


public class AppSettings {

    public int WindowTop { get; set; }
    public int WindowLeft { get; set; }
    public bool MinimizeToTray { get; set; }
    public int WindowHeight { get; set; }
    public int WindowWidth { get; set; }
    public DeleteFiles DeleteAfterUpload { get; set; }
    public bool PreMatchPage { get; set; }
    public bool PostMatchPage { get; set; }
    public string Theme { get; set; }

}
