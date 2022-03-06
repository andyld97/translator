using Translator.Model;
using Translator.Model.Log;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Translator.Model.Templates;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateNewProjectDialog.xaml
    /// </summary>
    public partial class CreateNewProjectDialog : Window
    {
        private bool isInitalized = false;

        public CreateNewProjectDialog()
        {
            InitializeComponent();
            TextProjectPath.Text = Settings.Instance.ProjectPath;
            isInitalized = true;
        }

        private void ButtonSearchProjectPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog flbd = new FolderBrowserDialog();
            var result = flbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                TextProjectPath.Text = flbd.SelectedPath;
        }

        private async void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrEmpty(TextName.Text))
            {
                // Project name is empty
                MessageBox.Show("Der Projekt-Name darf nicht leer sein!", "Leerer Projekt-Name", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (RecentlyOpenedProjects.Projects.Any(p => p.Name.ToLower() == TextName.Text.ToLower()))
            {
                // Project already exists
                MessageBox.Show("Der Projekt existiert bereits!", "Projekt existiert bereits", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(TextProjectPath.Text))
            {
                // Project Holder path is empty
                MessageBox.Show("Bitte wählen Sie eine gültige Projekt-Pfad-Ablage aus", "Leerer Pfad", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!System.IO.Directory.Exists(TextProjectPath.Text))
            {
                // Project Holder path doesn't exists
                MessageBox.Show("Die ausgewählte Projekt-Pfad-Ablage existiert nicht", "Pfad existiert nicht", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                Settings.Instance.ProjectPath = TextProjectPath.Text;
                Settings.Instance.Save();
            }

            if (string.IsNullOrEmpty(TextMainLanguage.Text) || TextMainLanguage.Text.Length != 2)
            {
                MessageBox.Show("Bitte geben Sie eine gültige Sprache im TwoLetterISOLang Format an!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(TextStringFormat.Text))
            {
                MessageBox.Show("Bitte geben Sie ein gültiges String-Format an!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
             
            if (string.IsNullOrEmpty(TextStringReplaceKey.Text))
            {
                MessageBox.Show("Bitte geben Sie ein gültiges Ersetzungsstring an!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(TextProjectUrl.Text) || TextProjectUrl.Text.Contains("www"))
            {
                MessageBox.Show("Bitte geben Sie eine gültige Projekt-Url an!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Project newProject = new Project
            {
                Name = TextName.Text,
                Description = TextDescription.Text,
                MainLanguage = TextMainLanguage.Text,
                DeepLApiKey = TextDeepLAPIKey.Text,
                GoogleCloudProjectID = TextGoogleCloudProjectID.Text,
                StringFormat = TextStringFormat.Text,
                StringReplaceKey = TextStringReplaceKey.Text,
                TelegramProtocolSendUrl = TextTelegramProtocolSendUrl.Text,
                ProjectUrl = TextProjectUrl.Text
            };

            // Definie pathes and templates
            string newProjectFolderPath = System.IO.Path.Combine(TextProjectPath.Text, newProject.Name);
            string templateName = CmbTemplate.SelectedIndex == 0 ? string.Empty : Model.Templates.Templates.DefaultTempalte.Name;
            var template = Templates.FindTemplate(templateName);

            try
            {
                // Create project directories
                newProject.ProjectFilePath = System.IO.Path.Combine(newProjectFolderPath, $"{newProject.Name}.tproj");

                string[] directoriesToCreate = new string[]
                {
                    newProjectFolderPath,
                    newProject.PublicFolder,
                    newProject.LocalizationsFolder,
                    newProject.ImagesFolder,
                    newProject.BlogImagesFolder,
                    newProject.TagImagesFolder,
                    newProject.ResourcesFolder,
                    newProject.BlogTemplateFolder,
                    newProject.RawTagFolder
                };
                foreach (var dir in directoriesToCreate)
                    System.IO.Directory.CreateDirectory(dir);

                // Add default html templates to the project directory
                System.IO.File.Copy(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", Consts.IndexFile), System.IO.Path.Combine(newProject.BlogTemplateFolder, Consts.IndexFile));
                System.IO.File.Copy(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", Consts.IndexDarkFile), System.IO.Path.Combine(newProject.BlogTemplateFolder, Consts.IndexDarkFile));

                newProject.Template = templateName;
                newProject.ApplyTemplate(newProject.Template);
                newProject.Save();

                // Create service account file (if any)
                if (!string.IsNullOrEmpty(template.GoogleCloudProjectFileContent))
                    await System.IO.File.WriteAllTextAsync(newProject.GoogleServiceAccountFilePath, template.GoogleCloudProjectFileContent.Replace("~", "\\n"));

                Logger.LogInformation($"Das Projekt \"{newProject.Name}\" wurde erfolgreich erstellt!", "ProjectCreator");
                Project.LoadProject(newProject.ProjectFilePath);
            }
            catch (Exception ex)
            {
                // Delete (or at least try to delete) created project folder!
                try
                {
                    System.IO.Directory.Delete(newProjectFolderPath, true);
                }
                catch
                {
                    // ignore
                }

                MessageBox.Show($"Fehler beim Erstellen des Projekts: {ex.Message}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CmbTemplate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!isInitalized)
                return;

            var templateName = CmbTemplate.SelectedIndex == 0 ? string.Empty : Model.Templates.Templates.DefaultTempalte.Name;
            Template temp = Templates.FindTemplate(templateName);

            TextDeepLAPIKey.Text = temp.DeepLKey;
            TextGoogleCloudProjectID.Text = temp.GoogleCloudProjectName;
            TextTelegramProtocolSendUrl.Text = temp.TelegramUrl;           
        }
    }
}