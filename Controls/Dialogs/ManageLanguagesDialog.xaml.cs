using System.Linq;
using System.Windows;
using Translator.Model;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für ManageLanguagesDialog.xaml
    /// </summary>
    public partial class ManageLanguagesDialog : Window
    {
        public ManageLanguagesDialog()
        {
            InitializeComponent();
            ListLanguages.ItemsSource = Project.CurrentProject.Languages;
        }

        #region Languages
        private void ButtonClearLanguages_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sind Sie sich wirklich sicher, dass Sie alle Sprachen löschen möchten? Alle Übersetzungen gehen dann verloren?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var lang in Project.CurrentProject.Languages)
                    MainWindow.W_INSTANCE.RefreshItemSource();

                foreach (var item in Project.CurrentProject.Items)
                    item.Translations.Clear();

                Project.CurrentProject.Languages.Clear();
                Project.CurrentProject.Save();
            }
        }

        private void ButtonDeleteLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (ListLanguages.SelectedIndex == -1)
                return;

            Language toDelete = Project.CurrentProject.Languages[ListLanguages.SelectedIndex];
            if (toDelete.LangCode == Project.CurrentProject.MainLanguage)
                return; // you cannot delete the default language

            if (MessageBox.Show($"Sind Sie sich wirklich sicher, dass Sie die Sprache \"{toDelete.Name}\" wirklich löschen möchten? Alle Übersetzungen gehen dann velroren?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Delete languages from each language item!
                foreach (var item in Project.CurrentProject.Items)
                {
                    var translation = item.Translations.Where(p => p.Language == toDelete.LangCode).FirstOrDefault();
                    if (translation != null)
                        item.Translations.Remove(translation);
                }

                // Delete language
                Project.CurrentProject.Languages.Remove(toDelete);
                Project.CurrentProject.Save();

               var assoc =  Project.CurrentProject.BlogItems.Where(p => p.LangCode == toDelete.LangCode).FirstOrDefault();
                if (assoc != null)
                    Project.CurrentProject.BlogItems.Remove(assoc);
                Project.CurrentProject.Save();

                // Delete also language row
                MainWindow.W_INSTANCE.RefreshItemSource();
                MainWindow.W_INSTANCE.RefreshBlogs();
                MainWindow.W_INSTANCE.RefreshTags();
                MainWindow.W_INSTANCE.CurrentSelectedBlogLanguage = Project.CurrentProject.Languages.Where(p => p.LangCode == Project.CurrentProject.MainLanguage).FirstOrDefault();
            }
        }

        private async void ButtonAddLanguage_Click(object sender, RoutedEventArgs e)
        {
            string newLangName = TextLangName.Text;
            string newLangCode = TextLangCode.Text;


            if (string.IsNullOrEmpty(newLangName) || string.IsNullOrEmpty(newLangCode))
            {
                MessageBox.Show("Bitte geben Sie gültige Werte ein!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Project.CurrentProject.Languages.Any(x => x.LangCode.ToLower() == newLangCode.ToLower() || x.Name.ToLower() == newLangName))
            {
                MessageBox.Show("Die Sprache existiert bereits!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var lang = new Model.Language(newLangName, newLangCode);
            Project.CurrentProject.Languages.Add(lang);
            // All items must have this language also!
            foreach (var li in Project.CurrentProject.Items)
                li.Translations.Add(new Translation(newLangCode, string.Empty));
            Project.CurrentProject.Save();
            MainWindow.W_INSTANCE.RefreshData();
            MainWindow.W_INSTANCE.RefreshTags();

            // Update grid columns (add this language column)
            MainWindow.W_INSTANCE.RefreshItemSource();

            TextLangCode.Clear();
            TextLangName.Clear();

            if (MessageBox.Show($"Die Sprache \"{lang.Name}\" wurde erfolgreich hinzugefügt, möchten Sie alle deutschen Blogs in diese Sprache übersetzen?", "Übersetzen?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var assoc = Project.CurrentProject.BlogItems.Where(p => p.LangCode == Project.CurrentProject.MainLanguage).FirstOrDefault();
                if (assoc != null)
                {
                    lang.IsSelected = true;
                    DialogResult = true;
                    await MainWindow.W_INSTANCE.TranslateBlogsAsync(assoc.Items.ToArray(), MainWindow.W_INSTANCE.CurrentSelectedBlogLanguage.LangCode, lang);
                }
            }
        }

        #endregion
    }
}
