using Translator.Helper;
using Translator.Model;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Threading.Tasks;

namespace Translator.Controls
{
    /// <summary>
    /// Interaktionslogik für SingleTranslation.xaml
    /// </summary>
    public partial class SingleTranslation : UserControl
    {
        private bool ignoreTextChanged = false;
        private bool isLoaded = false;
        private readonly Language currentLang = null;
        private readonly Item currentItem = null;

        public string LangCode => currentLang?.LangCode;

        public SingleTranslation(Language lang, Item item)
        {
            InitializeComponent();
            currentLang = lang;
            currentItem = item;

            TextLang.Text = lang.LangCode;
            TextLang.ToolTip = lang.Name;
            ignoreTextChanged = true;

            if (item.IsMultiLineText)
            {
                TextContent.Visibility = Visibility.Collapsed;
                TabHtml.Visibility = Visibility.Visible;
                TextHTML.ApplyTheming();
                TextHTML.Text = item.Translate(lang.LangCode);
                TextContent.Text = item.Translate(lang.LangCode);
            }
            else
            {
                TextContent.Visibility = Visibility.Visible;
                TabHtml.Visibility = Visibility.Collapsed;
                TextContent.Text = item.Translate(lang.LangCode);
                TextHTML.Text = item.Translate(lang.LangCode);
            }

            // Prevent confusion right there
            if (lang.LangCode == Project.CurrentProject.MainLanguage)
            {
                ButtonTranslate.IsEnabled = false;
                ButtonTranslate.Opacity = 0.4;
            }

            var translation = item.Translations.FirstOrDefault(p => p.Language == lang.LangCode);
            if (translation != null)
                ChkApprobed.IsChecked = translation.IsApproved;

            ignoreTextChanged = false;

            if (Consts.APP_VERSION == Consts.Version.Translator && !Consts.SupportedLanguages.Contains(lang))
            {
                if (lang.LangCode == Project.CurrentProject.MainLanguage)
                    TextContent.IsReadOnly = true;
                else
                {
                    Visibility = Visibility.Collapsed;
                    TextContent.IsEnabled = false;
                }
            }

            if (Consts.APP_VERSION == Consts.Version.Translator)
                ButtonTranslate.Cursor = System.Windows.Input.Cursors.No;

            if (Settings.Instance.HideApprovmentFeature)
                ChkApprobed.Visibility = Visibility.Collapsed;
        }

        private void TextContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ignoreTextChanged)
                return;

            string currentValue = TextContent.Text;
            if (currentItem != null && currentItem.MaxTextLength > 0 && currentValue.Length > currentItem.MaxTextLength)
            {
                currentValue = currentValue.Substring(0, (int)currentItem.MaxTextLength);
                ignoreTextChanged = true;
                TextContent.Text = currentValue;
                ignoreTextChanged = false;
            }

            currentItem?.SetTranslation(currentLang.LangCode, currentValue);
        }

        private async void ButtonTranslate_Click(object sender, RoutedEventArgs e)
        {
            await Translate();
        }

        public async Task Translate()
        {
            if (Consts.APP_VERSION == Consts.Version.Translator)
            {
                MessageBox.Show("Sie sind ein Übersetzer! Bitte erledigen Sie Ihre Arbeit, die automatischen Übersetzungen sollten bereits vorhanden sein und sollten von Ihnen korrigiert werden!", "Keine Berechtigung!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ProgressBarTextContent.Visibility = Visibility.Visible;

            try
            {
                if (!currentItem.IsMultiLineText)
                {
                    string temp = currentItem.Translate(Project.CurrentProject.MainLanguage);
                    temp = temp.Replace(Project.CurrentProject.StringFormat, Project.CurrentProject.StringReplaceKey);

                    var translationResult = await TranslationHelper.TranslateText(temp, Project.CurrentProject.MainLanguage, currentLang.LangCode, Settings.Instance.TranslationAPI);
                    if (!translationResult.Success)
                        throw translationResult.Exception;

                    TextContent.Text = translationResult.Text.Replace(Project.CurrentProject.StringReplaceKey, Project.CurrentProject.StringFormat);

                    if (!TextContent.Text.Contains(Project.CurrentProject.StringFormat) && temp.EndsWith(Project.CurrentProject.StringReplaceKey))
                        TextContent.Text = $"{TextContent.Text.Trim()} {Project.CurrentProject.StringFormat}";

                    // Auto Approbation??
                    // ChkApprobed.IsChecked = true;
                }
                else
                {
                    var translationResult = await TranslationHelper.TranslateHTML(currentItem.Translate(Project.CurrentProject.MainLanguage), Project.CurrentProject.MainLanguage, currentLang.LangCode, Settings.Instance.TranslationAPI);
                    if (!string.IsNullOrEmpty(translationResult))
                    {
                        TextHTML.Text = translationResult;

                        // Auto Approbation??
                        // ChkApprobed.IsChecked = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Fehler beim Übersetzen: {ex.Message}, {ex.InnerException}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally 
            {
                ProgressBarTextContent.Visibility = Visibility.Collapsed;
            }
        }

        private void ChkApprobed_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreTextChanged)
                return;

            currentItem?.SetApproved(currentLang.LangCode, ChkApprobed.IsChecked.Value);
        }

        private void ChkApprobed_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreTextChanged)
                return;

            currentItem?.SetApproved(currentLang.LangCode, ChkApprobed.IsChecked.Value);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (isLoaded || !currentItem.IsMultiLineText)
                return;

            isLoaded = true;
            PreviewHTML.LoadHtml(TextHTML.Text, string.Empty);          
        }

        private void TextHTML_TextChanged(object sender, System.EventArgs e)
        {
            if (!isLoaded || !currentItem.IsMultiLineText)
                return;

            if (currentItem != null)
                currentItem?.SetTranslation(currentLang.LangCode, TextHTML.Text);
        }

        private void TabHtml_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PreviewHTML.LoadHtml(TextHTML.Text, string.Empty);
        }

        public void SetReadOnly()
        {
            TextContent.IsReadOnly = true;
            TextHTML.IsReadOnly = true;
            ButtonTranslate.IsEnabled = false;
            ChkApprobed.IsEnabled = false;
        }

        public void SetWriteable()
        {
            TextContent.IsReadOnly = false;
            TextHTML.IsReadOnly = false;
            ButtonTranslate.IsEnabled = true;
            ChkApprobed.IsEnabled = true;
        }
    }
}
