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
using Serilog;

namespace HeroesProfile.Uploader.UI;

public class App : Application
{
    public new static App Current => (App)Application.Current!;

    public App()
    {
        Services = ConfigureServices(ApplicationLifetime);
    }

    public IServiceProvider Services { get; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            // var suspension = Services.GetRequiredService<AutoSuspendHelper>();
            // RxApp.SuspensionHost.CreateNewAppState = () => Services.GetRequiredService<MainWindowViewModel>();
            // RxApp.SuspensionHost.SetupDefaultSuspendResume(new SuspensionDriver("appstate.json"));
            // suspension.OnFrameworkInitializationCompleted();
            //
            // var state = RxApp.SuspensionHost.GetAppState<MainWindowViewModel>();

            CancellationTokenSource cts = new CancellationTokenSource();

            desktop.MainWindow = new MainWindow();
            desktop.Startup += async (sender, args) => {
                var manager = Services.GetRequiredService<IManager>();
                await manager.Start(cts.Token);
            };

            desktop.Exit += (sender, args) => {
                var manager = Services.GetRequiredService<IManager>();
                manager.Stop();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices(IApplicationLifetime? applicationLifetime)
    {
        var services = new ServiceCollection();

        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        services
            .AddSingleton(configuration)
            .AddSerilog(loggerConfiguration => {
                loggerConfiguration.ReadFrom.Configuration(configuration);
                loggerConfiguration.WriteTo.Console();
            })
            .Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)))
            .AddOptions<AppSettings>(nameof(AppSettings));


        if (Design.IsDesignMode) {
            services.AddSingleton<IManager, FakeManager>();
            services.AddSingleton<MainWindowViewModel>();
        } else {
#if DEBUG
            const string HeroesProfileApi = "http://127.0.0.1:8000/api";
            const string HeroesProfileWeb = "http://localhost/";
#else
            const string HeroesProfileApi = "https://api.heroesprofile.com/";
            const string HeroesProfileWeb = "https://www.heroesprofile.com/";
#endif

            services
                .AddHttpClient<PreMatchProcessor>(configureClient: static client => { client.BaseAddress = new Uri(HeroesProfileWeb, UriKind.Absolute); })
                .ConfigurePrimaryHttpMessageHandler(() => new MockServerHttpMessageHandler())
                .AddTypedClient<IPreMatchProcessor, PreMatchProcessor>();

            services
                .AddHttpClient<ReplayUploader>(configureClient: static client => { client.BaseAddress = new Uri(HeroesProfileApi, UriKind.Absolute); })
                .ConfigurePrimaryHttpMessageHandler(() => new MockServerHttpMessageHandler())
                .AddTypedClient<IReplayUploader, ReplayUploader>();

            services.AddSingleton<IManager, Manager>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<IReplayStorer, ReplayStorer>();
            services.AddSingleton<IGameMonitor, GameMonitor>();
            services.AddSingleton<IReplayAnalyzer, ReplayAnalyzer>();
        }

        services.PostConfigure<AppSettings>(options => { options.PostConfigure(); });


        return services.BuildServiceProvider();
    }
}