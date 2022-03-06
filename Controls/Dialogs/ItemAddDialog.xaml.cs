using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Translator.Model;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für ItemAddDialog.xaml
    /// </summary>
    public partial class ItemAddDialog : Window
    {
        public Item ResultItem { get; private set; } = new Item();
        private double oldHeight = 0;
        private bool isGeneral = false;

        public ItemAddDialog(Page currentSelectedPage, Scope currentSelectedScope)
        {
            InitializeComponent();
            CmbPages.ItemsSource = Project.CurrentProject.Pages;
            if (Project.CurrentProject.Pages.Count > 0)
                CmbPages.SelectedIndex = 0;

            CmbLanguages.ItemsSource = Project.CurrentProject.Languages;
            if (Project.CurrentProject.Languages.Count > 0)
                CmbLanguages.SelectedIndex = 0;

            int shouldBeLocked = 0;
            if (currentSelectedPage != Page.GENERAL)
            {
                foreach (var page in Project.CurrentProject.PagesAll)
                {
                    if (page.Scopes.Contains(currentSelectedScope) && currentSelectedPage != page)
                    {
                        ListBoxPages.Items.Add(page);
                        ResultItem.Pages.Add(page.ID);
                        shouldBeLocked++;
                    }
                }

                ListBoxPages.Items.Add(currentSelectedPage);
                ResultItem.Pages.Add(currentSelectedPage.ID);

                // ToDo: If the user adds a page or switches the category there is a problem:
                // In this category the user cannot add another page, because there are only the category in the selected pages
                // If the user switches the category, the list of pages may be changed??? Or the better way is to lock pages and lock category,
                // because the preselection depends on the "clicked" treeview item.

                // If we have a scope which has no parents (= it's used on all pages), we need to ensure ChckGeneral is checked!
                var parentPages = currentSelectedScope.GetParentPages();
                if (parentPages.Count == 0)
                    ChkGeneral.IsChecked = true;
            }
            else
                ChkGeneral.IsChecked = true;

            RefreshScopes();
            CmbScope.SelectedItem = currentSelectedScope;

            if (shouldBeLocked > 1)
            {
                CmbScope.IsEnabled =
                ListBoxPages.IsEnabled =
                ChkGeneral.IsEnabled =
                ButtonAddCurrentPage.IsEnabled =
                ButtonRemoveSelectedPage.IsEnabled =
                ButtonClear.IsEnabled = false;
            }

            Loaded += ItemAddDialog_Loaded;
        }

        private void ItemAddDialog_Loaded(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(TextKey), TextKey);
            Keyboard.Focus(TextKey);
        }

        private void ButtonAddCurrentPage_Click(object sender, RoutedEventArgs e)
        {
            if (CmbPages.SelectedIndex == -1)
                return;

            if (!ResultItem.DPages.Contains(Project.CurrentProject.Pages[CmbPages.SelectedIndex]))
            {
                var page = Project.CurrentProject.Pages[CmbPages.SelectedIndex];
                ResultItem.Pages.Add(page.ID);
                ListBoxPages.Items.Add(page);
            }
            else
                MessageBox.Show("Diese Seite existiert bereits in der Liste!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);

            RefreshScopes();
        }

        private void ButtonRemoveSelectedPage_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxPages.SelectedIndex == -1)
                return;

            int index = ListBoxPages.SelectedIndex;
            ResultItem.Pages.RemoveAt(index);
            ListBoxPages.Items.RemoveAt(index);
            RefreshScopes();
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            ResultItem.Pages.Clear();
            ListBoxPages.Items.Clear();
            RefreshScopes();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            // ToDo: *** Validate input, only chars and underscores

            // Validate input
            // Check key first
            string key = TextKey.Text;
            if (string.IsNullOrEmpty(key) || key.ToLower() == "general" || key == "*")
            {
                MessageBox.Show("Der Key darf nicht leer sein!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if ((!isGeneral && ResultItem.Pages.Count == 0))
            {
                MessageBox.Show("Sie haben keine Seiten ausgewählt!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (CmbScope.SelectedIndex == -1)
            {
                MessageBox.Show("Sie haben kein Bereich ausgewählt!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // To validate the key, we need to know Page(s) and Scope(s)
            // And also prepare result item
            Scope sp = CmbScope.SelectedItem as Scope;
            if (isGeneral)
            {
                // Page: *
                // Scope: * or example

                if (sp == Scope.GENERAL)
                {
                    // Page: *
                    // Scope: *
                    // Key must be unique
                    if (Project.CurrentProject.Items.Any(x => x.Key == key))
                    {
                        MessageBox.Show($"Der Key \"{key}\" existiert bereits und darf nicht erneut verwendet werden!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    ResultItem.ScopeID = sp.ID;
                }
                else
                {
                    // Page: *
                    // Scope: example
                    // Key must not exists twice in every page in the selected scope 
                    foreach (var page in Project.CurrentProject.PagesAll)
                    {
                        if (page.Scopes.Contains(sp) && page.GetItems(sp).Any(x => x.Key == key))
                        {
                            MessageBox.Show($"Der Key \"{key}\" existiert bereits und darf nicht erneut verwendet werden!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    ResultItem.ScopeID = sp.ID;
                }

                ResultItem.Pages.Clear();
                ResultItem.Pages.Add(Page.GENERAL.ID);
            }
            else
            {
                // Pages: 1,2
                // Scope: example
                foreach (var page in ResultItem.DPages)
                {
                    if (page.Scopes.Contains(sp) && page.GetItems(sp).Any(x => x.Key == key))
                    {
                        MessageBox.Show($"Der Key \"{key}\" existiert bereits und darf nicht erneut verwendet werden!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                ResultItem.ScopeID = sp.ID;
            }

            ResultItem.Key = key;
            ResultItem.IsMultiLineText = ChkIsLongText.IsChecked.Value;
            // ToDo: *** Add MaxLength

            // Ensure all languages are covered
            // Hint: Adding new languages => All items should then also get an entry for that language
            foreach (var item in Project.CurrentProject.Languages)
            {
                if (!ResultItem.Translations.Where(p => p.Language == item.LangCode).Any())
                    ResultItem.Translations.Add(new Translation(item.LangCode, string.Empty));
            }

            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonAddTranslation_Click(object sender, RoutedEventArgs e)
        {
            string text = TextTranslation.Text;
            if (!string.IsNullOrEmpty(text) && CmbLanguages.SelectedIndex != -1)
            {
                var lang = Project.CurrentProject.Languages[CmbLanguages.SelectedIndex];

                if (ResultItem.Translations.Any(p => p.Language == lang.LangCode))
                {
                    var translation = ResultItem.Translations.Where(p => p.Language == lang.LangCode).FirstOrDefault();
                    if (translation != null)
                    {
                        translation.Text = text;
                        ListTranslations.Items.Clear();

                        foreach (var item in ResultItem.Translations)
                        {
                            string langText = Project.CurrentProject.Languages.Where(p => p.LangCode == item.Language).Select(p => p.Name).FirstOrDefault();
                            ListTranslations.Items.Add($"{langText}: {item.Text}");
                        }
                    }
                }
                else
                {
                    ResultItem.Translations.Add(new Translation(lang.LangCode, text));
                    ListTranslations.Items.Add($"{lang.Name}: {text}");
                }

                TextTranslation.Clear();
            }
        }

        private void ButtonRemoveSelectedTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (ListTranslations.SelectedIndex != -1)
            {
                int index = ListTranslations.SelectedIndex;
                ListTranslations.Items.RemoveAt(index);
                ResultItem.Translations.RemoveAt(index);
            }
        }

        private void ButtonClearTranslations_Click(object sender, RoutedEventArgs e)
        {
            ListTranslations.Items.Clear();
            ResultItem.Translations.Clear();
        }

        private void RefreshScopes()
        {
            if (isGeneral)
            {
                List<Scope> scopes = new List<Scope>();
                scopes.AddRange(Project.CurrentProject.GeneralScopes);
                scopes.Add(Scope.GENERAL);
                CmbScope.ItemsSource = scopes.Distinct();
            }
            else
            {
                List<Scope> scopes = new List<Scope>();
                foreach (var page in ResultItem.DPages)
                    scopes.AddRange(page.ScopesAll);

                CmbScope.ItemsSource = scopes.Distinct();
            }

            if (CmbScope.SelectedIndex == -1)
                CmbScope.SelectedIndex = 0;
        }

        private void SetGeneral(bool state)
        {
            isGeneral = state;
            ButtonAddCurrentPage.Visibility = ButtonRemoveSelectedPage.Visibility = ButtonClear.Visibility = ListBoxPages.Visibility = CmbPages.Visibility = (!state ? Visibility.Visible : Visibility.Collapsed);
            RefreshScopes();
        }

        private void ChkGeneral_Checked(object sender, RoutedEventArgs e)
        {
            SetGeneral(ChkGeneral.IsChecked.Value);
        }

        private void ChkGeneral_Unchecked(object sender, RoutedEventArgs e)
        {
            SetGeneral(ChkGeneral.IsChecked.Value);
        }
        private void ChkIsLongText_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Grid.SetRow(TextTranslation, 1);
            System.Windows.Controls.Grid.SetColumnSpan(TextTranslation, 2);
            System.Windows.Controls.Grid.SetColumn(TextTranslation, 0);
            oldHeight = TextTranslation.Height;
            TextTranslation.Height = 135;
            TextTranslation.AcceptsReturn = true;
            TextTranslation.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Visible;
            TextTranslation.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Visible;
            System.Windows.Controls.Grid.SetColumn(CmbLanguages, 0);
            System.Windows.Controls.Grid.SetColumnSpan(CmbLanguages, 2);
        }

        private void ChkIsLongText_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Grid.SetRow(TextTranslation, 0);
            System.Windows.Controls.Grid.SetColumnSpan(TextTranslation, 1);
            System.Windows.Controls.Grid.SetColumn(TextTranslation, 1);
            TextTranslation.Height = oldHeight;
            TextTranslation.AcceptsReturn = false;
            TextTranslation.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Hidden;
            TextTranslation.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Hidden;
            System.Windows.Controls.Grid.SetColumn(CmbLanguages, 0);
            System.Windows.Controls.Grid.SetColumnSpan(CmbLanguages, 1);
        }
    }
}