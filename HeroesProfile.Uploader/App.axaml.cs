using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HeroesProfile.Uploader.Core;
using HeroesProfile.Uploader.Core.OS;
using HeroesProfile.Uploader.Core.OS.WinOS;
using HeroesProfile.Uploader.Core.Services;
using HeroesProfile.Uploader.Helpers;
using HeroesProfile.Uploader.Models;
using HeroesProfile.Uploader.ViewModels;
using HeroesProfile.Uploader.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HeroesProfile.Uploader;

public partial class App : Application
{
    public static string SettingsDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Heroesprofile");
    
    public static IHost? AppHost { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureLogging((context, logging) => logging.AddConsole())
            .ConfigureServices((context, services) => {

                if (Design.IsDesignMode) {
                    services.AddSingleton<DesignMainWindowViewModel>();
                } else {
                    services.AddSingleton<MainWindowViewModel>();
                }
                    
                #if MACOS
                services.AddSingleton<IStartupHelper, MacOsStartupHelper>();
                #elif WINDOWS
                services.AddSingleton<IStartupHelper, WindowsStartupHelper>();
                #endif
                
                services.AddSingleton<MainWindow>();
                services.AddSingleton<IGameFileMonitor, GameFileMonitor>();
                services.AddSingleton<IAnalyzer, Analyzer>();
                services.AddSingleton<IReplayTrackerStorage, ReplayTrackerStorage>();
                services.AddSingleton<IPreMatchProcessor, PreMatchProcessor>();
                services.AddSingleton<SettingsService<UserSettings>>();
                services.AddSingleton<IReplayUploader, ReplayUploader>();
                services.AddSingleton<Manager>();
            });

        AppHost = hostBuilder.Build();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            desktop.Startup += Startup;
            desktop.ShutdownRequested += ShutDownRequested;

            desktop.Exit += (sender, e) => {
                AppHost.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                AppHost.Dispose();
                AppHost = null;
            };
        }

        base.OnFrameworkInitializationCompleted();

        await AppHost.StartAsync();
    }

    private void ShutDownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        var manager = AppHost!.Services.GetRequiredService<Manager>();
        manager.Stop();
    }

    private async void Startup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        var manager = AppHost!.Services.GetRequiredService<Manager>();
        await manager.Start();
    }
}