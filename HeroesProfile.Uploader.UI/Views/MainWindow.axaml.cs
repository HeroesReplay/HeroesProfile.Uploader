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
        this.WhenActivated(disposable => { });

        AvaloniaXamlLoader.Load(this);

        if (Design.IsDesignMode) {
            Design.SetDataContext(this, App.Current.Services.GetRequiredService<MainWindowViewModel>());
        }
    }
}