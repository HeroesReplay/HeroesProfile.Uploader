using HeroesProfile.Uploader.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HeroesProfile.Uploader.Helpers;

public static class DesignData
{
    public static MainWindowViewModel MainWindowViewModel { get; } = App.AppHost!.Services.GetRequiredService<DesignMainWindowViewModel>();
}