using H.NotifyIcon;

using Heroesprofile.Uploader.Common;
using Heroesprofile.Uploader.Windows.Core;

using Microsoft.Extensions.Configuration;

using NLog;
using NLog.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Heroesprofile.Uploader.Windows
{
    public partial class App : Application
    {
        public new static App Current => (App) Application.Current;


        private static object _lock = new object();

        public IConfiguration Config { get; private set; }

        public Logger _log;
        
        public UserSettings UserSettings { get; private set; }
        public AppSettings AppSettings { get; private set; }

        private SettingsManager<UserSettings> settingsManager;

        public TaskbarIcon TaskbarIcon { get; private set; }
        public Manager Manager { get; private set; }
        
        public string SettingsDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Heroesprofile");

        public Version Version
        {
            get {
                if (ApplicationDeployment.IsNetworkDeployed) {
                    return ApplicationDeployment.CurrentDeployment.CurrentVersion;
                }

                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public string VersionString => $"v{Version}";

        public bool StartWithWindows
        {
            get {
                return StartupHelper.IsStartupEnabled();
            }
            set {
                if (value) {
                    StartupHelper.Add();
                } else {
                    StartupHelper.Remove();
                }
            }
        }

        public readonly Dictionary<string, string> Themes = new Dictionary<string, string> {
            { "Default", null },
            { "MetroDark", "Themes/MetroDark/MetroDark.Heroesprofile.Implicit.xaml" },
        };

       public App()
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            settingsManager = new SettingsManager<UserSettings>(Path.Combine(SettingsDir, "userSettings.json"));
            UserSettings = settingsManager.LoadSettings();
            AppSettings = Config.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

            if (UserSettings == null) {
                UserSettings = new UserSettings(AppSettings);
            }

            LogManager.Configuration = new NLogLoggingConfiguration(Config.GetSection("NLog"));
            _log = LogManager.GetCurrentClassLogger();

            if (ApplicationDeployment.IsNetworkDeployed) {
                _log.Info(JsonSerializer.Serialize(ApplicationDeployment.CurrentDeployment));
            } else {
                _log.Info("Not network deployed");
            }
        }


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            SetExceptionHandlers();
            _log.Info($"App {VersionString} started");

            SetupTrayIcon();
            Manager = new Manager(new ReplayStorage($@"{SettingsDir}\replays_v8.xml"));
            // Enable collection modification from any thread
            BindingOperations.EnableCollectionSynchronization(Manager.Files, _lock);

            Manager.PreMatchPage = UserSettings.PreMatchPage;
            Manager.PostMatchPage = UserSettings.PostMatchPage;
            Manager.DeleteAfterUpload = DeleteFiles.None;

            ApplyTheme(AppSettings.Theme);

            UserSettings.PropertyChanged += (o, ev) => {             

                if (ev.PropertyName == nameof(UserSettings.PreMatchPage)) {
                    Manager.PreMatchPage = UserSettings.PreMatchPage;
                } else if (ev.PropertyName == nameof(UserSettings.PostMatchPage)) {
                    Manager.PostMatchPage = UserSettings.PostMatchPage;
                }

                settingsManager.SaveSettings(UserSettings);
            };

            if (StartWithWindows) {
                // TODO: running in the background
            } else {
                MainWindow = new MainWindow();
                MainWindow.Deactivated += (o, ev) => {
                    // TODO: TrayIcon.Visible = true;
                };
                MainWindow.Show();
            }
            Manager.Start(new Monitor(), new LiveMonitor(), new Analyzer(), new Common.Uploader(), new LiveProcessor(Manager.PreMatchPage));
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            TaskbarIcon?.Dispose();
        }

        public void ApplyTheme(string theme)
        {
            // we will need a separate resource dictionary for themes 
            // if we intend to store someting else in App resource dictionary
            Resources.MergedDictionaries.Clear();
            Themes.TryGetValue(theme, out string resource);
            if (resource != null) {
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(resource, UriKind.Relative) });
            } else {
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("Themes/Default/Default.xaml", UriKind.Relative) });
            }
        }

        public void Activate()
        {   
            MainWindow = new MainWindow();
            MainWindow.Activate();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Show();

            // 
            // TaskbarIcon.Visible = false;
        }

        private void SetupTrayIcon()
        {
            TaskbarIcon = (TaskbarIcon)FindResource("NotifyIcon");
            TaskbarIcon.ForceCreate();
        }

        private void SetExceptionHandlers()
        {
            DispatcherUnhandledException += (o, e) => LogAndDisplay(e.Exception, "dispatcher");
            TaskScheduler.UnobservedTaskException += (o, e) => LogAndDisplay(e.Exception, "task");
            AppDomain.CurrentDomain.UnhandledException += (o, e) => LogAndDisplay(e.ExceptionObject as Exception, "domain");
        }

        private void LogAndDisplay(Exception e, string type)
        {
            _log.Error(e, $"Unhandled {type} exception");
            try {
                MessageBox.Show(e.ToString(), $"Unhandled {type} exception");
            }
            catch { /* probably not gui thread */ }
        }
    }
}