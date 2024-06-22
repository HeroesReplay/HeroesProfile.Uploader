using Heroesprofile.Uploader.Common;

using Microsoft.Extensions.Configuration;

using NLog;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Heroesprofile.Uploader.Windows
{

    public partial class App : Application, INotifyPropertyChanged
    {
        public static IConfiguration Config { get; private set; }

        public AppConfig AppConfig { get; private set; }

        public static bool StartWithWindowsCheckboxEnabled => true;

        public event PropertyChangedEventHandler PropertyChanged;

        public NotifyIcon TrayIcon { get; private set; }
        public Manager Manager { get; private set; }
        internal static Properties.Settings Settings => Uploader.Windows.Properties.Settings.Default;
        public static string AppExe => Assembly.GetExecutingAssembly().Location;
        public static string AppDir => Path.GetDirectoryName(AppExe);
        public static string AppFile => Path.GetFileName(AppExe);
        public static string SettingsDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Heroesprofile");

        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public string VersionString => $"v{Version.Major}.{Version.Minor}" + (Version.Build == 0 ? "" : $".{Version.Build}");
        
        public bool StartWithWindows
        {
            get {
                return StartupHelper.IsStartupTaskEnabled();
            }
            set {
                if (value) {
                    StartupHelper.CreateStartupTask();
                } else {
                    StartupHelper.RemoveStartupTask();
                }
            }
        }

        public readonly Dictionary<string, string> Themes = new Dictionary<string, string> {
            { "Default", null },
            { "MetroDark", "Themes/MetroDark/MetroDark.Heroesprofile.Implicit.xaml" },
        };

        private static Logger _log = LogManager.GetCurrentClassLogger();
        private static object _lock = new object();
        public MainWindow mainWindow;

        public App()
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(AppDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            AppConfig = Config.GetSection("AppConfig").Get<AppConfig>()!;

            Settings.WindowHeight = AppConfig.WindowHeight;
            Settings.WindowWidth = AppConfig.WindowWidth;
            Settings.WindowLeft = AppConfig.WindowLeft;
            Settings.WindowTop = AppConfig.WindowTop;
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

            Manager.PreMatchPage = Settings.PreMatchPage;
            Manager.PostMatchPage = Settings.PostMatchPage;
            Manager.DeleteAfterUpload = Settings.DeleteAfterUpload;

            ApplyTheme(Settings.Theme);


            Settings.PropertyChanged += (o, ev) => {
                if (ev.PropertyName == nameof(Settings.DeleteAfterUpload)) {
                    Manager.DeleteAfterUpload = Settings.DeleteAfterUpload;
                } else if (ev.PropertyName == nameof(Settings.Theme)) {
                    ApplyTheme(Settings.Theme);
                } else if (ev.PropertyName == nameof(Settings.PreMatchPage)) {
                    Manager.PreMatchPage = Settings.PreMatchPage;
                } else if (ev.PropertyName == nameof(Settings.PostMatchPage)) {
                    Manager.PostMatchPage = Settings.PostMatchPage;
                }
            };


            if (Settings.MinimizeToTray) {
                TrayIcon.Visible = true;
            } else {
                mainWindow = new MainWindow();
                mainWindow.Show();
            }
            Manager.Start(new Monitor(), new LiveMonitor(), new Analyzer(), new Common.Uploader(), new LiveProcessor(Manager.PreMatchPage));
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            TrayIcon?.Dispose();
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
            if (mainWindow != null) {
                if (mainWindow.WindowState == WindowState.Minimized) {
                    mainWindow.WindowState = WindowState.Normal;
                }
                mainWindow.Activate();
            } else {
                mainWindow = new MainWindow();
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                TrayIcon.Visible = false;
            }
        }

        private void SetupTrayIcon()
        {
            TrayIcon = new NotifyIcon {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = true
            };
            TrayIcon.Click += (o, e) => {
                if (mainWindow != null) {
                    mainWindow.Activate();
                    TrayIcon.Visible = false;
                    return;
                } else {
                    mainWindow = new MainWindow();
                    mainWindow.Show();
                    TrayIcon.Visible = false;
                }
            };
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