using System;
using System.Windows.Controls;
using Translator.Helper;
using Translator.Model.Log;

namespace Translator.Controls
{
    /// <summary>
    /// Interaction logic for AddImageControl.xaml
    /// </summary>
    public partial class AddImageControl : UserControl
    {
        public event EventHandler<string> ImageAdded;

        public AddImageControl()
        {
            InitializeComponent();
        }

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog() { Filter = "Bilder(*.bmp; *.png; *.jpg; *.jpeg; *.webp)| *.bmp; *.png; *.jpg; *.jpeg; *.webp" })
                {
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        ImageBorder.Child = new System.Windows.Controls.Image() { Source = ofd.FileName.LoadImageFromFileWithoutLocking(), Height = 200 };
                        ImageAdded?.Invoke(this, ofd.FileName);
                    }
                }
            }
        }

        public void Clear()
        {
            ImageBorder.Child = new System.Windows.Controls.TextBlock() { Text = "Bild einfügen ...", HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Center, FontSize = 38.0 };
        }

        public void LoadImage(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                Clear();
                return;
            }

            try
            {
                var image = path.LoadImageFromFileWithoutLocking();
                if (image != null)
                    ImageBorder.Child = new System.Windows.Controls.Image() { Source = image, Height = 200 };
                else
                    Clear();
            }
            catch (Exception ex)
            {
                Clear();
                Logger.LogError($"Fehler beim Öffnen des Bildes: {ex.Message}", "ImageControl");
            }            
        }
    }
}
