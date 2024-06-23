using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using System.Windows;

namespace Heroesprofile.Uploader.Windows.Core
{
    public partial class NotifyIconViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ShowWindowCommand))]
        public bool canExecuteShowWindow = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(HideWindowCommand))]
        public bool canExecuteHideWindow = true;

        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteShowWindow))]
        public void ShowWindow()
        {
            Application.Current.MainWindow ??= new MainWindow();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
            Application.Current.MainWindow.Show();
        }

        /// <summary>
        /// Hides the main window. This command is only enabled if a window is open.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteHideWindow))]
        public void HideWindow()
        {
            Application.Current.MainWindow.Hide(enableEfficiencyMode: true);
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        [RelayCommand]
        public void ExitApplication()
        {
            Application.Current.Shutdown();
        }
    }
}