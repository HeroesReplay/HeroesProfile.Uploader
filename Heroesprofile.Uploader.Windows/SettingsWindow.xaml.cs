using System.Windows;
using System.Windows.Input;

namespace Heroesprofile.Uploader.Windows;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        //if (App.Settings.AllowPreReleases) {
        //    PreReleasePanel.Visibility = Visibility.Visible;
        //}
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control) {
            PreReleasePanel.Visibility = Visibility.Visible;
        }
    }
}
