using ControlzEx.Theming;
using Translator.Model;
using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für BlogPreview.xaml
    /// </summary>
    public partial class BlogPreview : Window
    {
        public event EventHandler OnClosingPreviewWindow;

        public BlogPreview()
        {
            InitializeComponent();
            Loaded += BlogPreview_Loaded;
        }

        private async void BlogPreview_Loaded(object sender, RoutedEventArgs e)
        {
            var webView2Envoirnment = await CoreWebView2Environment.CreateAsync(null, Consts.WebView2CachePath);
            await Browser.EnsureCoreWebView2Async(webView2Envoirnment);

            Refresh();
        }

        public void Refresh()
        {
            string fileName = ThemeManager.Current.DetectTheme().BaseColorScheme == "Dark" ? Consts.IndexDarkFile : Consts.IndexFile;
            Browser.Source = new Uri(System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, fileName));
        }

        public void Clear()
        {
            Browser.Source = new Uri("about:blank");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            OnClosingPreviewWindow?.Invoke(this, EventArgs.Empty);
        }
    }
}