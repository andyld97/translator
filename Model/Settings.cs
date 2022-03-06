using IGPZ.Data.Serialization;
using Translator.Model.Log;
using System;

namespace Translator.Model
{
    public class Settings
    {
        private static readonly string DATA_PATH = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "settings_new.xml");

        public static readonly Settings Instance = Settings.Load();

        public LogLevelSetting LogLevel { get; set; } = LogLevelSetting.Production;

        public TranslationAPI TranslationAPI { get; set; } = TranslationAPI.DeepLFallback_GCloudTranslation;

        /// <summary>
        /// The last path which the user selected in the ProjectCreation-Dialog
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// If true, the most recent project will be loaded at startup
        /// </summary>
        public bool LoadLastProjectOnStartup { get; set; } = true;

        /// <summary>
        /// If true, the checkbox is not visible anymore
        /// </summary>
        public bool HideApprovmentFeature { get; set; } = true;

        /// <summary>
        /// Determines if the darkmode is active or not
        /// </summary>
        public bool UseDarkMode { get; set; } = false;

        /// <summary>
        /// The height of the log (GridSplitter)
        /// </summary>
        public double LogHeight { get; set; } = 250;

        /// <summary>
        /// The theme which is used in SimpleJournal (hard to describe, because Theme is enogugh)
        /// </summary>
        public string Theme { get; set; } = "Cobalt"; // Cobalt is the default one used in SJ

        public static Settings Load()
        {
            try
            {
                var temp = Serialization.Read<Settings>(DATA_PATH, Serialization.Mode.Normal);
                if (temp != null)
                    return temp;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fehler beim Laden der Settings: {ex.Message}", "Settings");
            }

            return new Settings();
        }

        public void Save()
        {
            try
            {
                Serialization.Save(DATA_PATH, this, Serialization.Mode.Normal);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fehler beim Speichern der Settings: {ex.Message}", "Settings");
            }
        }
    }
}
