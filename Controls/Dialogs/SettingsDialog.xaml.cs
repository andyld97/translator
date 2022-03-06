using Translator.Helper;
using Translator.Model;
using System.ComponentModel;
using System.Windows;
using ControlzEx.Theming;
using System.Linq;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window, INotifyPropertyChanged
    {
        private readonly bool editMode = false;
        private readonly bool changedWorkingDir = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public static Theme[] Themes
        {
            get
            {
                string value = Settings.Instance.UseDarkMode ? "Dark" : "Light";
                return ThemeManager.Current.Themes.Where(p => !p.DisplayName.Contains("Colorful") && p.DisplayName.Contains(value)).ToArray();
            }
        }

        public SettingsDialog()
        {
            editMode = true;
            InitializeComponent();

            TextFTPUsername.Text = Project.CurrentProject.TexterFTPUser;
            TextFTPServer.Text = Project.CurrentProject.TexterFTPHost;
            TextFTPPassword.Password = Project.CurrentProject.TexterFTPPassword.Decrypt();
            CmbSelectLogLevel.SelectedIndex = (int)Settings.Instance.LogLevel;
            CmbTranslatorAPI.SelectedIndex = (int)Settings.Instance.TranslationAPI;
            CheckOpenLastProjectOnStartup.IsChecked = Settings.Instance.LoadLastProjectOnStartup;
            TextDeepLAPIKey.Text = Project.CurrentProject.DeepLApiKey;
            TextGoogleCloudProjectID.Text = Project.CurrentProject.GoogleCloudProjectID;
            TextStringFormat.Text = Project.CurrentProject.StringFormat;
            TextStringReplaceKey.Text = Project.CurrentProject.StringReplaceKey;
            TextMainLanguage.Text = Project.CurrentProject.MainLanguage;
            TextTelegramProtocolSendUrl.Text = Project.CurrentProject.TelegramProtocolSendUrl;
            TextProjectUrl.Text = Project.CurrentProject.ProjectUrl;
            CheckHideApprovementFeature.IsChecked = Settings.Instance.HideApprovmentFeature;

            ComboBoxThemeChooser.DataContext = this;
            SelectTheme();

            editMode = false;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (ApplySettings())
                DialogResult = true;
            else
                ShowErrorMessage();
        }

        private bool ApplySettings()
        {
            if (editMode)
                return false;

            // Project level
            Project.CurrentProject.DeepLApiKey = TextDeepLAPIKey.Text;
            Project.CurrentProject.GoogleCloudProjectID = TextGoogleCloudProjectID.Text;
            Project.CurrentProject.StringFormat = TextStringFormat.Text;
            Project.CurrentProject.StringReplaceKey = TextStringReplaceKey.Text;
            Project.CurrentProject.MainLanguage = TextMainLanguage.Text;
            Project.CurrentProject.TelegramProtocolSendUrl = TextTelegramProtocolSendUrl.Text;
            Project.CurrentProject.ProjectUrl = TextProjectUrl.Text;

            // Project level - FTP
            Project.CurrentProject.TexterFTPUser = TextFTPUsername.Text;
            Project.CurrentProject.TexterFTPHost = TextFTPServer.Text;
            Project.CurrentProject.TexterFTPPassword = TextFTPPassword.Password.Encrypt();
            Project.CurrentProject.Save();

            // Settings level
            Settings.Instance.LogLevel = (LogLevelSetting)CmbSelectLogLevel.SelectedIndex;
            Settings.Instance.TranslationAPI = (TranslationAPI)CmbTranslatorAPI.SelectedIndex;
            Settings.Instance.LoadLastProjectOnStartup = CheckOpenLastProjectOnStartup.IsChecked.Value;
            Settings.Instance.HideApprovmentFeature = CheckHideApprovementFeature.IsChecked.Value;
            Settings.Instance.Save();

            return true;
        }

        private void SelectTheme()
        {
            // Get current selected theme
            string value = Settings.Instance.UseDarkMode ? "Dark" : "Light";
            CheckBoxDisplayMode.SelectedIndex = Settings.Instance.UseDarkMode ? 1 : 0;
            ComboBoxThemeChooser.SelectedItem = Themes.Where(p => p.DisplayName.Contains(Settings.Instance.Theme.Replace(".Colorful", string.Empty)) && p.DisplayName.Contains(value)).FirstOrDefault();
            CheckBoxThemeIsColorful.IsChecked = Settings.Instance.Theme.Contains(".Colorful");
        }

        private void UpdateThemeSettings()
        {
            if (ComboBoxThemeChooser.SelectedIndex == -1 || editMode)
                return;

            string theme = Themes[ComboBoxThemeChooser.SelectedIndex].Name.Replace("Light.", string.Empty).Replace("Dark.", string.Empty);

            if (CheckBoxThemeIsColorful.IsChecked.HasValue && CheckBoxThemeIsColorful.IsChecked.Value)
                theme += ".Colorful";

            Settings.Instance.Theme = theme;
            Settings.Instance.Save();

            // Apply theming
            GeneralHelper.ApplyTheming();
         
            // MainWindow.W_INSTANCE.UpdateGlowingBrush();
        }


        private void TextWorkspacePath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        private void TextFTPPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        private void TextWorkspacePath_TextChanged_1(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!ApplySettings())
            {
                e.Cancel = true;
                ShowErrorMessage();
            }
            else
            {
                if (changedWorkingDir)
                {
                    System.Diagnostics.Process.Start(System.Windows.Application.ResourceAssembly.Location);
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private void ShowErrorMessage()
        {
            System.Windows.MessageBox.Show("Bitte geben Sie gültige Pfade an!", "Ungültige Pfade!", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CmbSelectLogLevel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplySettings();
        }

        private void CmbTranslatorAPI_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplySettings();
        }

        private void CheckOpenLastProjectOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        private void TextMainLanguage_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        private void TextDeepLAPIKey_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        private void TextGoogleCloudProjectID_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        private void TextTelegramProtocolSendUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        private void TextStringFormat_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        private void TextStringReplaceKey_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        private void TextProjectUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySettings();
        }

        private void CheckHideApprovementFeature_Checked(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        private void CheckBoxDisplayMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (editMode)
                return;

            Settings.Instance.UseDarkMode = CheckBoxDisplayMode.SelectedIndex == 1;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Themes"));

            string value = Settings.Instance.UseDarkMode ? "Dark" : "Light";
            ComboBoxThemeChooser.ItemsSource = Themes;
            ComboBoxThemeChooser.SelectedItem = Themes.Where(p => p.DisplayName.Contains(Settings.Instance.Theme.Replace(".Colorful", string.Empty)) && p.DisplayName.Contains(value)).FirstOrDefault();

            Settings.Instance.Save();

            // Apply theming
            GeneralHelper.ApplyTheming();
            ApplySettings();
        }

        private void CheckBoxThemeIsColorful_Checked(object sender, RoutedEventArgs e)
        {
            if (editMode)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Themes"));
            UpdateThemeSettings();
        }

        private void ComboBoxThemeChooser_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (editMode || ComboBoxThemeChooser.SelectedIndex == -1)
                return;

            UpdateThemeSettings();
        }
    }
}