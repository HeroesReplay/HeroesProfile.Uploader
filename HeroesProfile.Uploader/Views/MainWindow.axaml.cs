using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using HeroesProfile.Uploader.ViewModels;

namespace HeroesProfile.Uploader.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void ShowLog_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}