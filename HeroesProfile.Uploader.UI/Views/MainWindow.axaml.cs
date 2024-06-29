using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using HeroesProfile.Uploader.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace HeroesProfile.Uploader.UI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        this.WhenActivated(disposable => { });
        ViewModel = App.Current.Services.GetRequiredService<MainWindowViewModel>();
    }
}