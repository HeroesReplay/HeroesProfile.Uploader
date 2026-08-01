using System;
using System.Windows;
using System.Windows.Input;

namespace Heroesprofile.Uploader.Windows
{
    public partial class WebhookDialog : Window
    {
        public string WebhookUrl { get; private set; }

        public WebhookDialog(string currentUrl)
        {
            InitializeComponent();
            UrlTextBox.Text = currentUrl ?? "";
            UrlTextBox.Focus();
            UrlTextBox.SelectAll();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlTextBox.Text.Trim();
            if (url != "" &&
                (!Uri.TryCreate(url, UriKind.Absolute, out Uri parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))) {
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            WebhookUrl = url;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) {
                DialogResult = false;
            }
        }
    }
}
