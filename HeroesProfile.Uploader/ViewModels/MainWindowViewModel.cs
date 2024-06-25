using System.Collections.ObjectModel;
using HeroesProfile.Uploader.Models;
using ReactiveUI;

namespace HeroesProfile.Uploader.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public ObservableCollection<StormReplayInfo> Files { get; } = new();
    
    public ObservableCollection<StormReplayProcessResult> ProcessResults { get; } = new();
    
    public MainWindowViewModel()
    {
        
        
    }
}