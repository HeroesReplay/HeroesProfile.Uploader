using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace HeroesProfile.Uploader.UI.ViewModels;

public class SuspensionDriver(string file) : ISuspensionDriver
{
    private readonly JsonSerializerOptions _settings = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IObservable<object> LoadState()
    {
        var json = System.IO.File.ReadAllText(file);
        return Observable.Return(JsonSerializer.Deserialize<object>(json, _settings))!;
    }

    public IObservable<Unit> SaveState(object state)
    {
        var json = JsonSerializer.Serialize(state, _settings);
        System.IO.File.WriteAllText(file, json);
        return Observable.Return(Unit.Default);
    }

    public IObservable<Unit> InvalidateState()
    {
        if (System.IO.File.Exists(file))
        {
            System.IO.File.Delete(file);
        }
        
        return Observable.Return(Unit.Default);
    }
}