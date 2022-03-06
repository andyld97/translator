using Translator.Helper;
using Translator.Model;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace Translator.Controls
{
    /// <summary>
    /// Interaktionslogik für BlogViewItem.xaml
    /// </summary>
    public partial class BlogViewItem : UserControl
    {
        public BlogViewItem()
        {
            InitializeComponent();
        }

        public void SetItem(BlogItem bg)
        {
            DataContext = bg;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start((sender as Hyperlink).NavigateUri?.AbsoluteUri);
            }
            catch
            {

            }
        }
    }

    #region Converter

    public class BlogHyperLinkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BlogItem bg)
            {
                return bg.GenerateLink(bg.Language);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BlogImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string str && values[1] is bool b)
            {
                try
                {
                    var image = ImageHelper.LoadImageFromFileWithoutLocking(System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, str));

                    if (!b)
                    {
                        FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap();
                        grayBitmap.BeginInit();
                        grayBitmap.Source = image;
                        grayBitmap.DestinationFormat = PixelFormats.Gray8;
                        grayBitmap.EndInit();

                        return grayBitmap;
                    }

                    return image;
                }
                catch
                {

                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MetaTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BlogItem bg && int.TryParse(parameter.ToString(), out int index))
            {
                if (bg.Tags.Count == 0 && index < bg.MetaInfo.Keywords.Length)
                    return bg.MetaInfo.Keywords[index];
                else if (index < bg.Tags.Count)
                    return Project.CurrentProject.Tags.Where(t => t.ID == bg.Tags[index]).FirstOrDefault()?.Name;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToVisiblityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BlogItem bg && int.TryParse(parameter.ToString(), out int index))
            {
                if (index < bg.MetaInfo.Keywords.Length && !string.IsNullOrEmpty(bg.MetaInfo.Keywords[index]))
                    return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
