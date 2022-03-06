using Translator.Controls.Dialogs;
using Translator.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Translator.Controls
{
    /// <summary>
    /// Interaktionslogik für StringListControl.xaml
    /// </summary>
    public partial class StringListControl : UserControl
    {
        public StringListControl()
        {
            InitializeComponent();
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            string result = InputDialog.ShowInputDialog("Metakeyword eingeben!", "Metakeyword oder eine Liste von Metakeywords angeben!");
            if (!string.IsNullOrEmpty(result))
            {
                string[] meta = System.Array.Empty<string>();

                if (!result.Contains(","))
                    meta = new string[1] { result };
                else
                    meta = result.Split(",".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);

                bool foundInvalidChars = false;
                for (int i = 0; i < meta.Length; i++)
                {
                    if (Consts.InvalidMetaChars.Any(p => meta[i].Contains(p)))
                    {
                        foundInvalidChars = true;
                        MessageBox.Show("Ungültige Zeichen erkannt!", "Ungültige Zeichen!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                if (foundInvalidChars)
                    return;

                for (int m = 0; m < meta.Length; m++)
                {
                    // Skip duplicates
                    if (ListStrings.Items.Contains(meta[m]))
                        continue;

                    ListStrings.Items.Add(meta[m]);
                    ListStrings.SelectedIndex = ListStrings.Items.Count - 1;
                    ListStrings.Focus();
                }
            }
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (ListStrings.SelectedIndex == -1)
                return;

            ListStrings.Items.RemoveAt(ListStrings.SelectedIndex);
        }

        private void ButtonMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ListStrings.SelectedIndex;

            if (selectedIndex == -1 || selectedIndex == 0)
                return;

            ListStrings.Items.Insert(selectedIndex - 1, ListStrings.Items[selectedIndex]);
            ListStrings.Items.RemoveAt(selectedIndex + 1);
            ListStrings.Focus();
        }

        private void ButtonMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ListStrings.SelectedIndex;

            if (selectedIndex == -1 || selectedIndex == ListStrings.Items.Count - 1)
                return;

            var item = ListStrings.Items[selectedIndex];
            ListStrings.Items.RemoveAt(selectedIndex);
            ListStrings.Items.Insert(selectedIndex + 1, item);
            ListStrings.SelectedIndex = selectedIndex + 1;
            ListStrings.Focus();
        }

        public void Clear()
        {
            ListStrings.Items.Clear();
        }

        public string[] GetResult()
        {
            List<string> items = new List<string>();
            foreach (var item in ListStrings.Items)
                items.Add(item.ToString());

            return items.ToArray();
        }

        public void SetItems(string[] items)
        {
            Clear();

            if (items == null)
                return;

            foreach (var item in items)
                ListStrings.Items.Add(item);
        }

        private void ListStrings_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }
}
