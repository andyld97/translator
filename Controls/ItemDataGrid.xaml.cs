using Translator.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Translator.Controls
{
    /// <summary>
    /// Interaktionslogik für ItemDataGrid.xaml
    /// </summary>
    public partial class ItemDataGrid : UserControl
    {
        public event EventHandler<Item> OnItemSelected;
        private Item currentSelectedItem = null;
        private bool ignoreTextChanged = false;

        public ItemDataGrid()
        {
            InitializeComponent();

            foreach (var lang in Project.CurrentProject.Languages)
                AddLanguage(lang);

            DataGridItems.SelectionChanged += DataGridItems_SelectionChanged;
        }

        private void DataGridItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Item i)
            {
                currentSelectedItem = i;
                OnItemSelected?.Invoke(this, i);

                if (i.IsMultiLineText)
                {
                    ignoreTextChanged = true;
                    System.Windows.Controls.Grid.SetRowSpan(DataGridItems, 1);
                    EditPanel.Visibility = Visibility.Visible;
                    CmbLanguages.ItemsSource = Project.CurrentProject.Languages;
                    if (Project.CurrentProject.Languages.Count > 0)
                        CmbLanguages.SelectedIndex = 0;

                    TextEditItem.Text = i.Translate(Project.CurrentProject.Languages[CmbLanguages.SelectedIndex], false);
                    ignoreTextChanged = false;
                }
                else
                {
                    System.Windows.Controls.Grid.SetRowSpan(DataGridItems, 2);
                    EditPanel.Visibility = Visibility.Collapsed;
                    TextEditItem.Clear();
                }

                return;
            }

            currentSelectedItem = null;
        }

        public void AddLanguage(Language language)
        {
            Binding mb = new Binding { Converter = new ItemToLangugeConverter(), Path = new PropertyPath("."), Mode = BindingMode.TwoWay };
            mb.ConverterParameter = language.LangCode;

            DataGridItems.Columns.Add(new System.Windows.Controls.DataGridTextColumn()
            {
                Header = new System.Windows.Controls.TextBlock() { Text = $"Sprache: {language.Name}", FontWeight = FontWeights.Bold, Tag = language.Name },
                Binding = mb,
                Width = 150,
            });
        }

        public void RemoveLangauge(Language language)
        {
            // Determine column which has this langauge
            DataGridColumn dataGridColumn = null;

            foreach (var column in DataGridItems.Columns)
            {
                if (column is DataGridTextColumn dtc && dtc.Header is TextBlock tb && tb.Tag != null && tb.Tag.ToString() == language.Name)
                {
                    dataGridColumn = dtc;
                    break;
                }
            }

            if (dataGridColumn != null)
                DataGridItems.Columns.Remove(dataGridColumn);
        }

        public void SetItemSource(IEnumerable<Item> languageItems)
        {     
            DataGridItems.ItemsSource = languageItems;

            currentSelectedItem = null;
            System.Windows.Controls.Grid.SetRowSpan(DataGridItems, 2);
            EditPanel.Visibility = Visibility.Collapsed;
            TextEditItem.Clear();
        }

        public void Refresh()
        {
            DataGridItems.Items.Refresh();
        }

        public void SetOnlyLangHeaders(bool state)
        {
            foreach (var column in DataGridItems.Columns)
            {
                if (column.Header is System.Windows.Controls.TextBlock tb && !tb.Text.Contains("Sprache"))
                    column.Visibility = (state ? Visibility.Collapsed : Visibility.Visible);

                // Key should always be visible
                if (column.Header is System.Windows.Controls.TextBlock t && t.Text == "Key")
                    column.Visibility = Visibility.Visible;
            }
        }

        private void ButtonEditPages_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button).Tag is Item li)
            {
                // ToDo: Show edit dialog
            }
        }

        private void TextEditItem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CmbLanguages.SelectedIndex == -1 || currentSelectedItem ==  null || ignoreTextChanged)
                return;

            var lang = Project.CurrentProject.Languages[CmbLanguages.SelectedIndex];

            if (currentSelectedItem.Translations.Any(x => x.Language == lang.LangCode))
                currentSelectedItem.Translations.Where(x => x.Language == lang.LangCode).First().Text = TextEditItem.Text;
            else
                currentSelectedItem.Translations.Add(new Translation(lang.LangCode, TextEditItem.Text));

            Project.CurrentProject.Save();
        }

        private void CmbLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbLanguages.SelectedIndex == -1 || currentSelectedItem == null)
                return;

            var lang = Project.CurrentProject.Languages[CmbLanguages.SelectedIndex];
            var translation = currentSelectedItem.Translations.Where(p => p.Language == lang.LangCode).FirstOrDefault();

            ignoreTextChanged = true;
            if (translation == null)
                TextEditItem.Clear();
            else
                TextEditItem.Text = translation.Text;
            ignoreTextChanged = false;
        }
    }

    public class ItemToLangugeConverter : IValueConverter
    {
        private Item li;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is Item li)
            {
                this.li = li;
                return li.Translate(parameter.ToString());
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (li == null)
                return null;

            var item = li.Translations.Where(p => p.Language == parameter.ToString()).FirstOrDefault();
            if (item != null)
            {
                item.Text = value.ToString();
                Project.CurrentProject.Save();
            }
            else
                li.Translations.Add(new Translation(parameter.ToString(), value.ToString()));

            return li;
        }
    }
}
