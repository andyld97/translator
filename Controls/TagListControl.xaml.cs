using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Translator.Model;
using Translator.Model.Tags;

namespace Translator.Controls
{
    /// <summary>
    /// Interaction logic for TagListControl.xaml
    /// </summary>
    public partial class TagListControl : UserControl
    {
        public TagListControl()
        {
            InitializeComponent();
            Refresh();
        }

        public void Refresh()
        {
            if (Project.CurrentProject == null)
                return;

            CmbTags.ItemsSource = Project.CurrentProject.Tags.OrderBy(t => t.Name).ToList();

            if (Project.CurrentProject.Tags.Count > 0)
                CmbTags.SelectedIndex = 0;
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (CmbTags.SelectedItem != null && CmbTags.SelectedItem is Tag t && !ListTags.Items.Contains(t))
                ListTags.Items.Add(t);
            else
                MessageBox.Show("Der Tag ist möglicherweise schon vorhanden!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (ListTags.SelectedIndex == -1)
                return;

            ListTags.Items.RemoveAt(ListTags.SelectedIndex);
        }

        private void ButtonMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ListTags.SelectedIndex;

            if (selectedIndex == -1 || selectedIndex == 0)
                return;

            ListTags.Items.Insert(selectedIndex - 1, ListTags.Items[selectedIndex]);
            ListTags.Items.RemoveAt(selectedIndex + 1);
            ListTags.Focus();
        }

        private void ButtonMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ListTags.SelectedIndex;

            if (selectedIndex == -1 || selectedIndex == ListTags.Items.Count - 1)
                return;

            var item = ListTags.Items[selectedIndex];
            ListTags.Items.RemoveAt(selectedIndex);
            ListTags.Items.Insert(selectedIndex + 1, item);
            ListTags.SelectedIndex = selectedIndex + 1;
            ListTags.Focus();
        }

        public void Clear()
        {
            ListTags.Items.Clear();
        }

        public Tag[] GetResult()
        {
            List<Tag> items = new List<Tag>();
            foreach (var item in ListTags.Items)
                items.Add(item as Tag);

            return items.ToArray();
        }

        public void SetItems(Tag[] items)
        {
            Clear();

            if (items == null)
                return;

            foreach (var item in items)
                ListTags.Items.Add(item);
        }

        private void ListTags_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
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
