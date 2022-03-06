using Translator.Model;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace Translator.Controls
{
    /// <summary>
    /// Interaktionslogik für TranslationItem.xaml
    /// </summary>
    public partial class TranslationItem : System.Windows.Controls.UserControl
    {
        private Page currentPage = null;
        private Item currentItem = null;

        private List<SingleTranslation> singleTranslations = new List<SingleTranslation>();

        public TranslationItem()
        {
            InitializeComponent();

            if (Consts.APP_VERSION == Consts.Version.Translator)
                TextItemPath.Cursor = System.Windows.Input.Cursors.No;
        }

        public void Initalize(Item item, Page page)
        {
            currentPage = page;
            currentItem = item;
            TextItemPath.Text = item.ToJsonPath(page);
            PanelSingleTranslations.Children.Clear();
            singleTranslations.Clear();

            foreach (var lang in Project.CurrentProject.Languages)
            {
                SingleTranslation st = new SingleTranslation(lang, item);
                PanelSingleTranslations.Children.Add(st);

                singleTranslations.Add(st);
            }
        }

        private void TextItemPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Consts.APP_VERSION == Consts.Version.Translator)
                return;

            MessageBox.Show("Hier soll man das Item später bearbeiten können!", "Jo!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImageCopyToClipboard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (currentItem == null)
                return;

            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                try
                {
                    Clipboard.SetText(currentItem.ToJsonPath(currentPage));
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Fehler beim Kopieren in die Zwischenablage: {ex.Message}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ButtonTranslateAll_Click(object sender, RoutedEventArgs e)
        {
            var items = singleTranslations.Where(s => s.LangCode != Project.CurrentProject.MainLanguage);
            foreach (var item in items)
                await item.Translate();
        }

        public void SetReadOnly()
        {
            ButtonTranslateAll.IsEnabled = false;
            foreach (var item in singleTranslations)
                item.SetReadOnly();
        }

        public void SetWriteable()
        {
            ButtonTranslateAll.IsEnabled = true;
            foreach (var item in singleTranslations)
                item.SetWriteable();
        }

    }
}
