using System.Windows;
using System.Windows.Input;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public string ITitle { get; set; }

        public string Message { get; set; }

        public string Result => txtResult.Text;

        public InputDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DismissDialog();
        }

        private void DismissDialog()
        {
            if (!string.IsNullOrEmpty(Result))
                this.DialogResult = true;
            else
                MessageBox.Show(this, "Bitte geben Sie gültigen Text ein!", "Leerer Text!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void txtResult_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                DismissDialog();
        }

        public static string ShowInputDialog(string title, string message)
        {
            InputDialog inputDialog = new InputDialog
            {
                ITitle = title,
                Message = message
            };

            var result = inputDialog.ShowDialog();
            if (result.HasValue && result.Value)
                return inputDialog.Result;

            return string.Empty;
        }
    }
}
