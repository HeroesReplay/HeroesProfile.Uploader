using Avalonia.ReactiveUI;
using HeroesProfile.Uploader.Helpers;
using HeroesProfile.Uploader.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HeroesProfile.Uploader.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        if (Avalonia.Controls.Design.IsDesignMode) {
            this.DataContext = DesignData.MainWindowViewModel;
        }
        else {
            this.ViewModel = App.AppHost!.Services.GetRequiredService<MainWindowViewModel>();
        }
    }
}