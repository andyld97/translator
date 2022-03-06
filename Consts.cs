using Translator.Model;
using System;
using System.Collections.ObjectModel;

namespace Translator
{
    public class Consts
    {
        public static readonly DateTime ReleaseDate = new DateTime(2022, 06, 03, 22, 30, 0);

        public static readonly Version APP_VERSION
#if TRANSLATOR
            = Version.Translator;
#else
            = Version.Admin;
#endif

        /// <summary>
        /// Returns all supported languages (only required in translator-edition)
        /// </summary>
        public static ObservableCollection<Language> SupportedLanguages { get; } = new ObservableCollection<Language>();

        public static readonly string ChangelogUrl = "https://code-a-software.net/translator/chg.php?lang={0}&dark={1}";
        public static readonly string DonwloadUrl = "https://code-a-software.net/translator/download.php";
        public static readonly string VersionUrl = "https://code-a-software.net/translator/version.txt";
        public static readonly string BlogDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss''zz:00";
        public static readonly string WEBP = ".webp";
        public static readonly string IndexFile = "index.html";
        public static readonly string IndexDarkFile = "index_dark.html";
        public static readonly string HTMLLightTheme = "HTML";
        public static readonly string HTMLDarkTheme = "HTMLDARK";
        public static readonly int LastRecentlyOpenedDocuments = 10;
        public static readonly string WebView2CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Translator");
        public static readonly char[] InvalidMetaChars = new char[] { '\'', '"', ',' };

#if TRANSLATOR

        static Consts()
        {
            // If we want to compile a translator version just call AddLanguages with all languages
            // e.g. for en: AddLanguage("de"); AddLanguage("en");
        }

        private static void AddLanguage(string code)
        {
            if (SupportedLanguages.Any(x => x.LangCode == code))
                return;

            if (!Project.CurrentProject.Languages.Any(x => x.LangCode == code))
                return;

            var lang = Project.CurrentProject.Languages.Where(p => p.LangCode == code).FirstOrDefault();
            SupportedLanguages.Add(lang);
        }
#endif

        public enum Version
        {
            Admin,
            Translator
        }
    }
}