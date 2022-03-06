using ControlzEx.Theming;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Translator.Model;

namespace Translator.Controls
{
    /// <summary>
    /// Interaction logic for TextEditor.xaml
    /// </summary>
    public partial class TextEditor : UserControl, INotifyPropertyChanged
    {
        private bool useBlogs = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<EventArgs> TextChanged;

        #region Properties

        public string Text
        {
            get => TextContent?.Text;
            set => TextContent.Text = value;
        }

        public bool IsReadOnly
        {
            get
            {
                if (TextContent == null)
                    return false;

                return TextContent.IsReadOnly;
            }
            set
            {
                if (value != TextContent.IsReadOnly)
                {
                    TextContent.IsReadOnly = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("IsWriteable");
                }
            }
        }

        public bool UseBlogs
        {
            get => useBlogs;
            set
            {
                if (value != useBlogs)
                {
                    useBlogs = value;

                    if (value)
                    {
                        CmbBlogsEntrys.Visibility =
                        ButtonPasteBlogEntry.Visibility =
                        BlogMenu.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BlogMenu.Visibility =
                        ButtonPasteBlogEntry.Visibility =
                        CmbBlogsEntrys.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        public bool IsWriteable => !IsReadOnly;

        #endregion

        public TextEditor()
        {
            InitializeComponent();
            TextContent.ShowLineNumbers = true;
            TextContent.TextChanged += TextContent_TextChanged;
            DataContext = this;
        }

        #region Insert HTML Tags
        private void TextBlogContentButtonAddP_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("p");
        }

        private void TextBlogContentButtonAddH1_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("h1");
        }

        private void TextBlogContentButtonAddH2_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("h2");
        }
        private void TextBlogContentButtonAddH3_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("h3");
        }

        private void TextBlogContentButtonAddH4_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("h4");
        }

        private void TextBlogContentButtonAddH5_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("h5");
        }

        private void TextBlogContentButtonAddH6_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("h6");
        }

        private void TextBlogContentButtonAddB_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("b");
        }

        private void TextBlogContentButtonAddU_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("u");
        }

        private void TextBlogContentButtonAddI_Click(object sender, RoutedEventArgs e)
        {
            InsertHTMLTag("i");
        }

        private void InsertHTMLTag(string tag)
        {
            string formattedTag = $"<{tag}></{tag}>";

            TextContent.TextArea.Document.Insert(TextContent.CaretOffset, formattedTag);
            TextContent.TextArea.Caret.Offset -= tag.Length + 3;
        }

        #endregion

        #region Insert Blog

        public void RefreshBlogs(Language lang)
        {
            var items = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang?.LangCode).FirstOrDefault();

            if (items != null)
            {
                // Generate blog menu insert items
                BlogMenu.Items.Clear();
                CmbBlogsEntrys.Items.Clear();
                foreach (var blg in Project.CurrentProject.BlogItems.Where(p => p.LangCode == Project.CurrentProject.MainLanguage))
                {
                    foreach (var b in blg.Items)
                    {
                        var item = new System.Windows.Controls.MenuItem() { Header = b.Title, Tag = b, Foreground = new SolidColorBrush(Colors.Black) };
                        BlogMenu.Items.Add(item);

                        item.Click += Item_Click;
                        CmbBlogsEntrys.Items.Add(b);
                    }
                }

                var source = items.Items.OrderBy(p => p.PublishedDate).ToList();
                CmbBlogsEntrys.SelectedIndex = 0;
            }
        }

        private void Item_Click(object sender, RoutedEventArgs e)
        {
            if (TextContent.IsReadOnly)
                return;

            var blogItem = (sender as System.Windows.Controls.MenuItem).Tag as BlogItem;
            InsertBlogId(blogItem);
        }

        private void ButtonPasteBlogEntry_Click(object sender, RoutedEventArgs e)
        {
            if (CmbBlogsEntrys.SelectedIndex == -1 || !CmbBlogsEntrys.IsEnabled)
                return;

            InsertBlogId(CmbBlogsEntrys.Items[CmbBlogsEntrys.SelectedIndex] as BlogItem);
        }

        private void InsertBlogId(BlogItem blogItem)
        {
            if (blogItem == null)
                return;

            TextContent.TextArea.Document.Insert(TextContent.CaretOffset, $"<a href=\"{blogItem.ID}\">{blogItem.Title}</a>");
        }

        #endregion

        #region Wrapper Methods
        public void Clear()
        {
            TextContent.Clear();
        }

        private void TextContent_TextChanged(object sender, EventArgs e)
        {
            TextChanged?.Invoke(this, e);
        }

        public void ApplyTheming()
        {
            if (Settings.Instance.UseDarkMode)
                TextContent.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(Consts.HTMLDarkTheme);
            else
                TextContent.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(Consts.HTMLLightTheme);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}