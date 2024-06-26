using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using HeroesProfile.Uploader.Core;
using HeroesProfile.Uploader.Core.Services;
using HeroesProfile.Uploader.Models;
using ReactiveUI;

namespace HeroesProfile.Uploader.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly ReadOnlyObservableCollection<StormReplayInfo> _files;
    public ReadOnlyObservableCollection<StormReplayInfo> Files => _files;
    
    public ObservableCollectionEx<StormReplayProcessResult> ProcessResults { get; } = new();
    
    public MainWindowViewModel(Manager manager)
    {
        manager.Files.Connect()
            .Bind(out _files)
            .Subscribe();
    }
    
    private bool _launchOnStartup;
    public bool LaunchOnStartup
    {
        get => false;
        set => this.RaiseAndSetIfChanged(ref _launchOnStartup, value);
    }
}