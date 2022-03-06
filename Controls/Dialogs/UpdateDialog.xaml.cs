using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;
using Translator.Helper;
using Translator.Model;
using Translator.Model.Log;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for UpdateDialog.xaml
    /// </summary>
    public partial class UpdateDialog : Window
    {
        public UpdateDialog(Version v)
        {
            InitializeComponent();
            string text = "Version {0} ist jetzt verfügbar!";
            try
            {
                txtVersion.Text = string.Format(text, v.ToString(4));
                Logger.LogInformation(text, "Updater");
            }
            catch
            {
                txtVersion.Text = string.Format(text, v.ToString(3));
                Logger.LogInformation(text, "Updater");
            }

            Loaded += UpdateDialog_Loaded;
        }

        private async void UpdateDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load changelog
                var webView2Envoirnment = await CoreWebView2Environment.CreateAsync(null, Consts.WebView2CachePath);
                await webChangelog.EnsureCoreWebView2Async(webView2Envoirnment);
                webChangelog.Source = new Uri(string.Format(Consts.ChangelogUrl, "de", Settings.Instance.UseDarkMode ? 1 : 0));
            }
            catch (Exception)
            { }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GeneralHelper.OpenUri(new Uri(Consts.DonwloadUrl));
                DialogResult = true;

                // Exit to make sure user can easily update without problems
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Fehler beim Öffnen der Url: {Consts.DonwloadUrl} ({ex.Message})", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
