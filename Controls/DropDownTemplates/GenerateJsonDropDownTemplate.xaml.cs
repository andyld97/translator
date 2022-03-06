using System.Windows;

namespace Translator.Controls.DropDownTemplates
{
    /// <summary>
    /// Interaktionslogik für GenerateJsonDropDownTemplate.xaml
    /// </summary>
    public partial class GenerateJsonDropDownTemplate : DropDownTemplate
    {
        public delegate void onJsonGeneration(JsonGeneration mode);
        public event onJsonGeneration OnJsonGeneration;

        public GenerateJsonDropDownTemplate()
        {
            InitializeComponent();
        }

        private void ButtonGenerate_Click(object sender, RoutedEventArgs e)
        {
            CloseDropDown();
            OnJsonGeneration?.Invoke((JsonGeneration)CmbMode.SelectedIndex);
        }
    }
}
