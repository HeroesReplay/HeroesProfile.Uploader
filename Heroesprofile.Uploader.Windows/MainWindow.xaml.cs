using Heroesprofile.Uploader.Common;
using Heroesprofile.Uploader.Windows.Core;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Heroesprofile.Uploader.Windows
{
    public partial class MainWindow : Window
    {
        public string VersionString => App.Current.VersionString;

        public UserSettings UserSettings => App.Current.UserSettings;

        public ObservableCollectionEx<ReplayFile> Files => App.Current.Manager.Files;

        public Manager Manager => App.Current.Manager;

        public bool StartWithWindows
        {
            get => App.Current.StartWithWindows;
            set => App.Current.StartWithWindows = value;
        }


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        private void ShowLog_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", $@"{App.Current.SettingsDir}\logs");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow() { Owner = this, DataContext = this };
            settings.ShowDialog();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (App.Current.UserSettings.MinimizeToTray) {
                e.Cancel = true;
                Hide();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (App.Current.UserSettings.MinimizeToTray) {
                Hide();
            }                
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
