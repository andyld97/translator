using Translator.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Translator.Controls.DropDownTemplates
{
    /// <summary>
    /// Interaktionslogik für TranslateBlogDropDownTemplate.xaml
    /// </summary>
    public partial class TranslateBlogDropDownTemplate : DropDownTemplate
    {
        private readonly List<Language> languages;

        public List<Language> Result => languages;

        public delegate void startTranslation(List<Language> result, string sourceLanguage);
        public event startTranslation OnStartedTranslation;


        public TranslateBlogDropDownTemplate()
        {
            InitializeComponent();
            if (Project.CurrentProject != null)
                languages = Project.CurrentProject.Languages.Where(p => p.LangCode != Project.CurrentProject.MainLanguage).ToList();
            else
                languages = new List<Language>();

            ListLang.ItemsSource = languages;
            SelectLanguages(true);
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (!languages.Any(l => l.IsSelected))
            {
                CloseDropDown();
                MessageBox.Show("Sie müssen mindestens eine Sprache auswählen", "Keine Sprache ausgewählt!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CloseDropDown();
            OnStartedTranslation?.Invoke(Result, MainWindow.W_INSTANCE.CurrentSelectedBlogLanguage.LangCode);
        }

        private void ButtonClearSelection_Click(object sender, RoutedEventArgs e)
        {
            SelectLanguages(false);
        }

        private void ButtonSelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectLanguages(true);
        }

        private void SelectLanguages(bool state)
        {
            foreach (var lang in languages)
                lang.IsSelected = state;
        }
    }
}
