using ControlzEx.Theming;
using System;
using System.Net;
using System.Windows;
using System.Windows.Media;
using Translator.Controls.Dialogs;
using Translator.Model;

namespace Translator.Helper
{
    public class GeneralHelper
    {
        public static string GetCurrentTheme()
        {
            return $"{(Settings.Instance.UseDarkMode ? ThemeManager.BaseColorDarkConst : ThemeManager.BaseColorLightConst)}.{Settings.Instance.Theme}"; //.Colorful";
        }

        public static void ApplyTheming()
        {
            System.Windows.Media.Color translationColor;

            if (Settings.Instance.UseDarkMode)
                translationColor = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#2E2E2E");
            else
                translationColor = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F0F0F0");

            // Apply own theming colors
            App.Current.Resources["Item.TranslationColor"] = new SolidColorBrush(translationColor);
            ThemeManager.Current.ChangeTheme(Application.Current, GetCurrentTheme());
        }

        public static bool IsConnectedToInternet()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SearchForUpdates()
        {
            // Search for updates
            if (GeneralHelper.IsConnectedToInternet())
            {
                // If a new update is available
                // Display all changes from current version till new version (changelog is enough)

                // 1) Get current version
                var currentVersion = typeof(GeneralHelper).Assembly.GetName().Version;

                // 2) Download version
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        string versionString = wc.DownloadString(Consts.VersionUrl);
                        Version onlineVersion = Version.Parse(versionString);

                        var result = onlineVersion.CompareTo(currentVersion);
                        if (result > 0)
                        {
                            // There is a new version
                            UpdateDialog ud = new UpdateDialog(onlineVersion);
                            ud.ShowDialog();
                        }
                        else if (result < 0)
                        {
                            // Online version is older than this version (impossible case)
                        }
                        else
                        {
                            // equal
                        }
                    }
                }
                catch (Exception)
                {
                    // ignore failed to get updates
                }
            }
        }

        /// <summary>
        /// Opens the default system browser with the requested uri
        /// https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp/67838115#67838115
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static bool OpenUri(Uri uri)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"\"{uri}\"");
                return true;
            }
            catch (Exception)
            {
                // ignore 
                return false;
            }
        }
    }
}