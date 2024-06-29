using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HeroesProfile.Uploader.Models;

public sealed class UserSettings
{
    [JsonPropertyName("windowTop")]
    public required int WindowTop { get; set; }

    [JsonPropertyName("windowLeft")]
    public required int WindowLeft { get; set; }

    [JsonPropertyName("minimizeToTray")]
    public required bool MinimizeToTray { get; set; }

    [JsonPropertyName("windowHeight")]
    public required int WindowHeight { get; set; }

    [JsonPropertyName("windowWidth")]
    public required int WindowWidth { get; set; }

    [JsonPropertyName("preMatchPage")]
    public required bool PreMatchPage { get; set; }

    [JsonPropertyName("postMatchPage")]
    public required bool PostMatchPage { get; set; }

    public UserSettings()
    {

    }
}