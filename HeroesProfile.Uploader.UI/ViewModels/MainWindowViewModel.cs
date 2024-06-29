using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Binding;
using HeroesProfile.Uploader.Models;
using HeroesProfile.Uploader.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace HeroesProfile.Uploader.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase, IActivatableViewModel
{
    private ObservableCollection<StormReplayProcessResult> _results = new();
    public ObservableCollection<StormReplayProcessResult> Results => _results;

    private ReadOnlyObservableCollection<StormReplayInfo> _files;
    public ReadOnlyObservableCollection<StormReplayInfo> Files => _files;

    private bool _preMatchPage;
    private bool _postMatchPage;
    private bool _launchOnStart;
    private bool _runInBackground;

    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IManager _manager;
    private readonly UserSettingsStorage _userSettingsStorage;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IManager manager,
        UserSettingsStorage userSettingsStorage)
    {
        _logger = logger;
        _manager = manager;
        _userSettingsStorage = userSettingsStorage;

        this.WhenActivated(disposables => {

            if (Design.IsDesignMode) return;
            
            PreMatchPage = _userSettingsStorage.UserSettings!.PreMatchPage;
            PostMatchPage = _userSettingsStorage.UserSettings.PostMatchPage;
            RunInBackground = _userSettingsStorage.UserSettings.MinimizeToTray;

            PropertyChanged -= OnPropertyChanged;
            PropertyChanged += OnPropertyChanged;

            Disposable
                .Create(() => { })
                .DisposeWith(disposables);
        });

        _manager.Files.Connect()
            .AutoRefreshOnObservable(item => item.WhenAnyPropertyChanged())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Sort(new StormReplayInfoComparer())
            .Bind(out _files)
            .Subscribe(x => {
                var results = _files.GroupBy(f => f.UploadStatus)
                    .Select(g => new StormReplayProcessResult(g.Key, g.Count()))
                    .ToList();

                if (_results.Count == 0) {
                    _results.AddRange(results);
                } else {
                    foreach (var result in results) {
                        var existing = _results.FirstOrDefault(r => r.UploadStatus == result.UploadStatus);

                        if (existing != null) {
                            existing.Count = result.Count;
                        } else {
                            _results.Add(result);
                        }
                    }
                }
            });
    }

    private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(RunInBackground)) {
            _userSettingsStorage.UserSettings.MinimizeToTray = RunInBackground;
            await _userSettingsStorage.SaveAsync();
        }

        if (args.PropertyName == nameof(LaunchOnStart)) {
            _userSettingsStorage.UserSettings.LaunchOnStart = LaunchOnStart;
            await _userSettingsStorage.SaveAsync();
        }

        if (args.PropertyName == nameof(PreMatchPage)) {
            _userSettingsStorage.UserSettings.PreMatchPage = PreMatchPage;
            await _userSettingsStorage.SaveAsync();
        }

        if (args.PropertyName == nameof(PostMatchPage)) {
            _userSettingsStorage.UserSettings.PostMatchPage = PostMatchPage;
            await _userSettingsStorage.SaveAsync();
        }
    }

    public bool PreMatchPage
    {
        get => _preMatchPage;
        set {
            this.RaiseAndSetIfChanged(ref _preMatchPage, value);
            _manager.PreMatchPage = _preMatchPage;
        }
    }

    public bool PostMatchPage
    {
        get => _postMatchPage;
        set {
            this.RaiseAndSetIfChanged(ref _postMatchPage, value);
            _manager.PostMatchPage = _postMatchPage;
        }
    }

    public bool RunInBackground
    {
        get => _runInBackground;
        set => this.RaiseAndSetIfChanged(ref _runInBackground, value);
    }

    public bool LaunchOnStart
    {
        get => _launchOnStart;
        set => this.RaiseAndSetIfChanged(ref _launchOnStart, value);
    }

    public void OpenLogsCommand()
    {
        _logger.LogInformation("Opening logs directory");

        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Heroesprofile", "logs");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Process.Start("explorer.exe", path);
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            Process.Start("open", path);
        } else {
            throw new NotSupportedException("Unsupported operating system");
        }
    }

    public void OpenReplaysCommand()
    {
        _logger.LogInformation("Opening replays directory");

        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Process.Start("explorer.exe", path);
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            Process.Start("open", path);
        } else {
            throw new NotSupportedException("Unsupported operating system");
        }
    }

    public ViewModelActivator Activator { get; set; }= new ViewModelActivator();
}