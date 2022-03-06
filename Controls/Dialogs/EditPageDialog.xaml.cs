using Translator.Model;
using System.Windows;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für EditPageDialog.xaml
    /// </summary>
    public partial class EditPageDialog : Window
    {
        private readonly Page currentPage = null;
        private bool ignoreSelectionChanged = false;
        private bool isInitalized = false;

        public EditPageDialog(Page currentPage) // cannot be general
        {
            InitializeComponent();
            this.currentPage = currentPage;

            ignoreSelectionChanged = true;
            ListBoxDisplayPages.ItemsSource = Project.CurrentProject.Pages;
            if (Project.CurrentProject.Pages.Count > 0) // this should be anyway true, otherwise this dialog doesn't show up
                ListBoxDisplayPages.SelectedIndex = Project.CurrentProject.Pages.IndexOf(currentPage);
            TextName.Text = currentPage.Name;
            ignoreSelectionChanged = false;
            isInitalized = true;
        }

        private void ListBoxDisplayPages_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (currentPage == null || ignoreSelectionChanged)
                return;

            ignoreSelectionChanged = true;
            ListBoxDisplayPages.SelectedIndex = Project.CurrentProject.Pages.IndexOf(currentPage);
            ignoreSelectionChanged = false;
        }

        private void MovePage(Direction direction)
        {
            int currentIndex = Project.CurrentProject.Pages.IndexOf(currentPage);
            int count = Project.CurrentProject.Pages.Count;

            int nextIndex;
            if (direction == Direction.MoveUP)
            {
                if (currentIndex - 1 < 0)
                    nextIndex = count - 1;
                else
                    nextIndex = currentIndex - 1;
            }
            else
            {
                if (currentIndex + 1 == count)
                    nextIndex = 0;
                else
                    nextIndex = currentIndex + 1;
            }

            Project.CurrentProject.Pages.Move(currentIndex, nextIndex);
            Project.CurrentProject.Save();
        }

        private void ButtonMoveItemUp_Click(object sender, RoutedEventArgs e)
        {
            MovePage(Direction.MoveUP);
        }

        private void ButtonMoveItemDown_Click(object sender, RoutedEventArgs e)
        {
            MovePage(Direction.MoveDOWN);
        }

        public enum Direction
        {
            MoveUP,
            MoveDOWN
        }

        private void TextName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!isInitalized)
                return;

            currentPage.Name = TextName.Text;
            ListBoxDisplayPages.Items.Refresh();
            ListBoxDisplayPages.SelectedIndex = Project.CurrentProject.Pages.IndexOf(currentPage);
            Project.CurrentProject.Save();
        }
    }
}
