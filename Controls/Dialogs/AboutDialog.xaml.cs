using Microsoft.Web.WebView2.Core;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Translator.Helper;
using Translator.Model;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : Window
    {
        private bool isLoaded;

        public AboutDialog()
        {
            InitializeComponent();

            TextVersion.Text = typeof(AboutDialog).Assembly.GetName().Version.ToString();
            TextReleaseDate.Text = $"{Consts.ReleaseDate:g} Uhr";

            TextDotNetVersion.Text = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Loaded += AboutDialog_Loaded;
        }

        private async void AboutDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Prevent multiple calls
            if (isLoaded)
                return;

            isLoaded = true;

            await Initialize();
        }

        public async Task Initialize()
        {
            var currentVersion = typeof(AboutDialog).Assembly.GetName().Version;

            try
            {
                // Load version
                using (HttpClient httpClient = new HttpClient())
                {
                    string version = await httpClient.GetStringAsync(Consts.VersionUrl);
                    Version newVersion = Version.Parse(version);
   
                    if (currentVersion == newVersion)
                        TextVersion.Text += $" - aktuell!";
                    else
                    {
                        if (currentVersion > newVersion)
                        {
                            TextNewVersionAvailable.Text = "Unveröffentlichte Entwicklungsversion!";
                            TextNewVersionAvailable.Foreground = new SolidColorBrush(Colors.Red);
                        }
                        else
                            TextNewVersionAvailable.Text = $"*** Es gibt eine neuere Version: {newVersion} ***";
                    }
                }
            }
            catch (Exception)
            { }

            try
            {
                // Load changelog
                var webView2Envoirnment = await CoreWebView2Environment.CreateAsync(null, Consts.WebView2CachePath);
                await BrowserChangelog.EnsureCoreWebView2Async(webView2Envoirnment);
                BrowserChangelog.Source = new Uri(string.Format(Consts.ChangelogUrl, "de", Settings.Instance.UseDarkMode ? 1 : 0));
            }
            catch (Exception)
            { }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            GeneralHelper.OpenUri(e.Uri);
        }      
    }
}
