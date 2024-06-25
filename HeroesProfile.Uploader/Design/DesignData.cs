using HeroesProfile.Uploader.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HeroesProfile.Uploader.Design;

public static class DesignData
{
    public static MainWindowViewModel MainWindowViewModel { get; } = App.AppHost!.Services.GetRequiredService<DesignMainWindowViewModel>();
}