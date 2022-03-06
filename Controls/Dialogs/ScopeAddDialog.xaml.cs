using Translator.Model;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für ScopeAddDialog.xaml
    /// </summary>
    public partial class ScopeAddDialog : Window
    {
        private readonly Page currentSelectedPage = null;

        public ObservableCollection<Page> Result { get; set; } = new ObservableCollection<Page>();

        public string ResultName => TextScopeName.Text;

        private bool isGeneral = false; 

        public ScopeAddDialog(Page page)
        {
            currentSelectedPage = page;
            InitializeComponent();

            CmbPages.ItemsSource = Project.CurrentProject.Pages; // not pages all!!
            if (Project.CurrentProject.Pages.Count > 0)
                CmbPages.SelectedIndex = 0;

            if (currentSelectedPage != null)
                Result.Add(currentSelectedPage);

            if (currentSelectedPage == Page.GENERAL)
            {
                ChkGeneral.IsChecked = true;
                return;
            }

            ListBoxPages.ItemsSource = Result;
            Loaded += ScopeAddDialog_Loaded;
        }

        private void ScopeAddDialog_Loaded(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(TextScopeName), TextScopeName);
            Keyboard.Focus(TextScopeName);
        }

        private void ButtonAddCurrentPage_Click(object sender, RoutedEventArgs e)
        {
            // Add if its not tehre
            if (CmbPages.SelectedIndex > -1 && !Result.Contains(Project.CurrentProject.Pages[CmbPages.SelectedIndex]))
                Result.Add(Project.CurrentProject.Pages[CmbPages.SelectedIndex]);
            else
                MessageBox.Show("Dieses Item existiert bereits in der Liste!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ButtonRemoveSelectedPage_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxPages.SelectedIndex != -1 && ListBoxPages.Items.Count > 0)
                Result.RemoveAt(ListBoxPages.SelectedIndex);
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            Result.Clear();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (isGeneral)
                Result = new ObservableCollection<Page>() { Page.GENERAL };

            if (string.IsNullOrEmpty(ResultName))
            {
                MessageBox.Show("Der Name darf nicht leer sein!", "Bitte verwenden Sie einen vernünftigen Namen!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ResultName.ToLower() == Scope.GENERAL.Name.ToLower() || ResultName.ToLower() == Scope.GENERAL.DisplayName.ToLower())
            {
                MessageBox.Show($"Der Name {ResultName} darf nicht verwendet werden!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if this scope existis already in the given pages
            foreach (var page in Result)
            {
                if (page.Scopes.Any(x => x.DisplayName == ResultName || x.Name == ResultName))
                {
                    MessageBox.Show("Dieser Bereich existiert bereits in einer der ausgewählten Seiten!", "Bitte wählen Sie einen anderen Namen!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            this.DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void SetGeneral(bool state)
        {
            isGeneral = state;
            ButtonAddCurrentPage.IsEnabled = ButtonRemoveSelectedPage.IsEnabled = ButtonClear.IsEnabled = ListBoxPages.IsEnabled = CmbPages.IsEnabled = !state;
        }

        private void ChkGeneral_Checked(object sender, RoutedEventArgs e)
        {
            SetGeneral(ChkGeneral.IsChecked.Value);
        }

        private void ChkGeneral_Unchecked(object sender, RoutedEventArgs e)
        {
            SetGeneral(ChkGeneral.IsChecked.Value);
        }
    }
}
