using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using DynamicData;
using DynamicData.Binding;
using HeroesProfile.Uploader.Models;
using HeroesProfile.Uploader.Services;
using ReactiveUI;

namespace HeroesProfile.Uploader.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<StormReplayProcessResult> _results = new();
    public ObservableCollection<StormReplayProcessResult> Results => _results;

    private ReadOnlyObservableCollection<StormReplayInfo> _files;
    public ReadOnlyObservableCollection<StormReplayInfo> Files => _files;

    private string _status;
    private bool _preMatchPage;
    private bool _postMatchPage;

    private readonly IManager _manager;

    public MainWindowViewModel(IManager manager)
    {
        _manager = manager;
        _status = "Idle";

        _manager.Files.Connect()
            .AutoRefreshOnObservable(item => item.WhenAnyPropertyChanged())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Sort(new StormReplayInfoComparer())
            .Bind(out _files)
            .Subscribe(x => {
                _results.Clear();
                _files.GroupBy(f => f.UploadStatus)
                    .Select(g => new StormReplayProcessResult(g.Key, g.Count()))
                    .ToList()
                    .ForEach(f => _results.Add(f));

                this.RaisePropertyChanged(nameof(Results));
            });
    }

    private void OnItemChanged(IChangeSet<StormReplayInfo> changeSet)
    {
        Status = Files.Any(x => x.UploadStatus == UploadStatus.InProgress) ? "Uploading..." : "Idle";
    }

    public bool PreMatchPage
    {
        get => _preMatchPage;
        set {
            _manager.PreMatchPage = value;
            this.RaiseAndSetIfChanged(ref _preMatchPage, value);
        }
    }

    public bool PostMatchPage
    {
        get => _postMatchPage;
        set {
            _manager.PostMatchPage = value;
            _postMatchPage = value;
            this.RaiseAndSetIfChanged(ref _postMatchPage, value);
        }
    }

    public void OpenLogsCommand()
    {
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
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Process.Start("explorer.exe", path);
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            Process.Start("open", path);
        } else {
            throw new NotSupportedException("Unsupported operating system");
        }
    }

    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }
}