using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HeroesProfile.Uploader.Models;
using HeroesProfile.Uploader.Services;
using HeroesProfile.Uploader.UI.Fakes;
using HeroesProfile.Uploader.UI.ViewModels;
using HeroesProfile.Uploader.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace HeroesProfile.Uploader.UI;

public class App : Application
{
    public new static App Current => (App)Application.Current!;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public App()
    {
        Services = ConfigureServices(ApplicationLifetime);
    }

    public IServiceProvider Services { get; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.Startup += OnDesktopOnStartup;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnDesktopOnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs args)
    {
        if (sender is IClassicDesktopStyleApplicationLifetime desktop) {
            var logger = Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Starting application");

            var userSettingsStorage = Services.GetRequiredService<UserSettingsStorage>();
            await userSettingsStorage.LoadAsync();
            logger.LogInformation("User settings loaded");

            var manager = Services.GetRequiredService<IManager>();
            manager.IsPostMatchEnabled = userSettingsStorage.UserSettings!.IsPostMatchEnabled;
            manager.IsPreMatchEnabled = userSettingsStorage.UserSettings.IsPreMatchEnabled;
            await manager.StartAsync(_cancellationTokenSource.Token);

            desktop.MainWindow = new MainWindow() { DataContext = Services.GetRequiredService<MainWindowViewModel>() };
            desktop.MainWindow.Show();
        }
    }

    private static IServiceProvider ConfigureServices(IApplicationLifetime? applicationLifetime)
    {
        var services = new ServiceCollection();

        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        AppSettings appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;
        appSettings.CreateAppDataIfNotExists();

        services
            .AddLogging()
            .AddSingleton(configuration)
            .AddSingleton(appSettings)
            .AddSingleton<UserSettingsStorage>()
            .AddSerilog(loggerConfiguration => {
                loggerConfiguration.ReadFrom.Configuration(configuration);
                loggerConfiguration.WriteTo.File(
                    path: appSettings.HeroesProfileAppData.FullName + "/log.txt");
            });

        if (Design.IsDesignMode) {
            services.AddSingleton<IManager, FakeManager>();
        } else {
            services.AddSingleton<IManager, Manager>();
        }

        services
            .AddHttpClient<PreMatchProcessor>(configureClient: client => client.BaseAddress = new Uri(appSettings.HeroesProfileWebUrl, UriKind.Absolute))
            .AddTypedClient<IPreMatchProcessor, PreMatchProcessor>()
            .ConfigurePrimaryHttpMessageHandler(() => new MockServerHttpMessageHandler());

        services.AddHttpClient<PostMatchProcessor>(configureClient: client => client.BaseAddress = new Uri(appSettings.HeroesProfileApiUrl, UriKind.Absolute))
            .ConfigurePrimaryHttpMessageHandler(() => new MockServerHttpMessageHandler())
            .AddTypedClient<IPostMatchProcessor, PostMatchProcessor>();

        services
            .AddHttpClient<ReplayUploader>(configureClient: client => client.BaseAddress = new Uri(appSettings.HeroesProfileApiUrl, UriKind.Absolute))
            .ConfigurePrimaryHttpMessageHandler(() => new MockServerHttpMessageHandler())
            .AddTypedClient<IReplayUploader, ReplayUploader>();


        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IReplayStorer, ReplayStorer>();
        services.AddSingleton<IFileMonitor, FileMonitor>();
        services.AddSingleton<IReplayAnalyzer, ReplayAnalyzer>();

        services.PostConfigure<AppSettings>(options => { options.CreateAppDataIfNotExists(); });

        return services.BuildServiceProvider();
    }
}