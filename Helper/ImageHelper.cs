using Translator.Model.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace Translator.Helper
{
    public static class ImageHelper
    {
        private static readonly Dictionary<string, BitmapImage> flagCache = new Dictionary<string, BitmapImage>();

        public static BitmapImage LoadImageFromFileWithoutLocking(this string path)
        {
            if (!System.IO.File.Exists(path))
                return null;

            if (!string.IsNullOrEmpty(path))
            {
                BitmapImage image = new BitmapImage();
                using (FileStream stream = File.OpenRead(path))
                {
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                }
                return image;
            }

            return null;
        }

        public static string LoadImageFromFileWithoutLockingBase64(this string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                BitmapImage image = new BitmapImage();
                using (FileStream stream = File.OpenRead(path))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        long count = 0;
                        byte[] buffer = new byte[4096];
                        while (count < stream.Length)
                        {
                            if (count + buffer.Length > stream.Length)
                                buffer = new byte[stream.Length - count];

                            count += stream.Read(buffer, 0, buffer.Length);
                            ms.Write(buffer, 0, buffer.Length);
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }

            return null;
        }

        public static BitmapImage LoadFlag(string twoLetterLangCode)
        {
            if (flagCache.ContainsKey(twoLetterLangCode))
                return flagCache[twoLetterLangCode];

            try
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bi.UriSource = new System.Uri($"pack://application:,,,/Translator;component/resources/icons/flags/{twoLetterLangCode}.png");
                bi.EndInit();

                flagCache.Add(twoLetterLangCode, bi);

                return bi;
            }
            catch
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bi.UriSource = new System.Uri($"pack://application:,,,/Translator;component/resources/icons/flags/no_flag.png");
                bi.EndInit();

                flagCache.Add(twoLetterLangCode, bi);

                return bi;
            }
        }

        public static void CreateWebPImage(string imagePath, string webPImagePath)
        {
            try
            {
                if (!System.IO.File.Exists(imagePath))
                {
                    Logger.LogWarning($"Konvertierung von \"{imagePath}\" abgebrochen, die Datei existiert nicht!", "WebPConverter");
                    return;
                }
                else
                    Logger.LogInformation($"Konvertiere {imagePath} nach {webPImagePath} ...", "WebPConverter");

                System.Diagnostics.Process p = new System.Diagnostics.Process()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cwebp", "cwebp.exe"),
                        Arguments = $"\"{imagePath}\" -o \"{webPImagePath}\"",
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fehler beim Konvertieren des Bildes \"{imagePath}\" nach \"{webPImagePath}\": {ex.Message}", "WebPConverter");
            }
        }
    }
}
