using IGPZ.Data.Serialization;
using Translator.Controls.Dialogs;
using Translator.Model.Blog;
using Translator.Model.Log;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;
using Translator.Model.Templates;
using System.Linq;
using Translator.Model.Tags;

namespace Translator.Model
{
    public class Project
    {
        #region Event
        public delegate void onProjectChanged();
        public static event onProjectChanged OnProjectChanged;

        #endregion

        public static Project CurrentProject { get; set; }

        #region Propreties

        public string Name { get; set; }

        public string Description { get; set; }

        public string ProjectUrl { get; set; }

        public string TexterFTPHost { get; set; }

        public string TexterFTPUser { get; set; }

        public string TexterFTPPassword { get; set; }

        public string DeepLApiKey { get; set; }

        public string GoogleCloudProjectID { get; set; }

        public string TelegramProtocolSendUrl { get; set; }

        public string StringReplaceKey { get; set; } = "~";

        public string StringFormat { get; set; } = "%s";

        public string MainLanguage { get; set; } = "de";

        public string Template { get; set; }

        public ObservableCollection<Item> Items { get; set; } = new ObservableCollection<Item>();

        public ObservableCollection<Page> Pages { get; set; } = new ObservableCollection<Page>();

        public ObservableCollection<Language> Languages { get; set; } = new ObservableCollection<Language>();

        public ObservableCollection<BlogItemLanguageAssoc> BlogItems { get; set; } = new ObservableCollection<BlogItemLanguageAssoc>();

        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();

        public ObservableCollection<Scope> GeneralScopes { get; set; } = new ObservableCollection<Scope>();

        [XmlIgnore]
        public ObservableCollection<Page> PagesAll
        {
            get
            {
                ObservableCollection<Page> pages = new ObservableCollection<Page>();
                foreach (var page in Pages)
                    pages.Add(page);

                pages.Add(Page.GENERAL);

                return pages;
            }
        }

        #endregion

        #region Load/Save
        public static bool LoadProject(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath) || !System.IO.File.Exists(projectFilePath))
                return false;

            bool result = false;
            LoadingDialog loadingDialog = null;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                loadingDialog = new LoadingDialog(projectFilePath);
            }, DispatcherPriority.Background);

            loadingDialog.OnLoaded += async delegate {

                try
                {
                    var project = await Task.Run(() => Serialization.Read<Project>(projectFilePath));
                    if (project != null)
                    {
                        if (CurrentProject != null && CurrentProject.Name == project.Name)
                        {
                            MessageBox.Show($"Das Projekt \"{project.Name}\" ist bereits geladen!", "Projekt bereits geladen", MessageBoxButton.OK, MessageBoxImage.Error);
                            result = false;
                            loadingDialog.FinishDialog();
                            return;
                        }

                        // Important
                        project.ProjectFilePath = projectFilePath;

                        // Set google service account file (if any)
                        if (System.IO.File.Exists(project.GoogleServiceAccountFilePath))
                            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", project.GoogleServiceAccountFilePath);

                        CurrentProject = project;
                        OnProjectChanged?.Invoke();
                        Logger.LogInformation($"Das Projekt \"{CurrentProject.Name}\" wurde erfolgreich geladen!", "ProjectLoader");
                        result = true;
                    }
                }
                catch
                {

                }

                loadingDialog.FinishDialog();
            };
            System.Windows.Application.Current.Dispatcher.Invoke(() => {

                loadingDialog.ShowDialog();
            }, DispatcherPriority.Background);
            
            return result;
        }

        public void Save()
        {
            try
            {
                Serialization.Save(ProjectFilePath, this);
            }
            catch
            { }
        }
        #endregion

        #region Fixed Properties/Pathes

        [XmlIgnore]
        public string ProjectFilePath { get; set; }

        [XmlIgnore]
        public string ProjectPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ProjectFilePath));

        #region Public Folders

        [XmlIgnore]
        public string PublicFolder => System.IO.Path.Combine(ProjectPath, "public");

        [XmlIgnore]
        public string LocalizationsFolder => System.IO.Path.Combine(PublicFolder, "localizations");

        [XmlIgnore]
        public string ImagesFolder => System.IO.Path.Combine(PublicFolder, "images");

        [XmlIgnore]
        public string BlogImagesFolder => System.IO.Path.Combine(ImagesFolder, "blog");

        [XmlIgnore]
        public string TagImagesFolder => System.IO.Path.Combine(ImagesFolder, "tag");

        #endregion

        #region Resource Folders

        [XmlIgnore]
        public string ResourcesFolder => System.IO.Path.Combine(ProjectPath, "resources");

        [XmlIgnore]
        public string GoogleServiceAccountFilePath => System.IO.Path.Combine(ResourcesFolder, "google-service-account-file.json");

        [XmlIgnore]
        public string BlogTemplateFolder => System.IO.Path.Combine(ResourcesFolder, "blog-templates");

        [XmlIgnore]
        public string RawTagFolder => System.IO.Path.Combine(ResourcesFolder, "images", "raw-tag");

        #endregion

        #endregion

        #region Apply Template

        public void ApplyTemplate(string templateName)
        {
            if (templateName == null)
                return;

            Template template = Templates.Templates.FindTemplate(templateName);

            // Apply languages
            foreach (var lang in template.Languages)
                Languages.Add(lang);

            // Apply pages
            if (template.Pages != null)
            {
                foreach (var page in template.Pages)
                    AddPage(page, template);
            }

            // Apply general items
            foreach (var item in template.TemplateItems)
            {
                Item newItem = new Item() { Key = item.Name, IsMultiLineText = item.IsHtml };

                if (item.ItemType == ItemType.GeneralGeneral)
                    newItem.Pages.Add(Page.GENERAL.ID);
                else if (item.ItemType == ItemType.Scopes)
                    newItem.ScopeID = (Guid)(Pages.SelectMany(s => s.GetAllScopes(this)).FirstOrDefault(f => f.Name == item.ParentScopes.FirstOrDefault())?.ID);
                else if (item.ItemType == ItemType.GeneralPage)
                {
                    foreach (var pg in Pages)
                    {
                        var pgItem = new Item() { Key = item.Name, IsMultiLineText = item.IsHtml };
                        pgItem.Pages.Add(pg.ID);

                        AddTranslations(pgItem, pg, item.Value);
                        Items.Add(pgItem);
                    }

                    continue;
                }

                // Apply values
                AddTranslations(newItem, null, item.Value);

                // Add item
                Items.Add(newItem);
            }
        }

        public void AddPage(string name, string templateName)
        {
            AddPage(name, Templates.Templates.FindTemplate(templateName));
        }

        public void AddPage(string name, Template template)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (PagesAll.Any(x => x.DisplayName == name || x.Name == name))
                {
                    MessageBox.Show("Bitte verwenden Sie einen gültigen Seiten-Namen!", "Ungültiger Seiten-Name!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var page = new Page(name);
                ApplyTemplate(page, template);
                Pages.Add(page);
                Save();
            }
        }

        private void ApplyTemplate(Page page, Template template)
        {
            if (page == null || template == null)
                return;

            foreach (var scope in template.TemplateScopes)
            {
                Scope sc = new Scope(scope.Name);

                if (scope.ScopeType == ScopeType.General)
                {
                     if (!GeneralScopes.Any(p => p.Name == sc.Name))
                        GeneralScopes.Add(sc);
                }
                else if (scope.ScopeType == ScopeType.SinglePage && (scope.ParentPages == null || scope.ParentPages != null && scope.ParentPages.Any(p => p == page.Name)))
                    page.Scopes.Add(sc);
                else if (scope.ScopeType == ScopeType.MultipleParents)
                {
                    if (!scope.ParentPages.Any() || scope.ParentPages.Contains(page.Name))
                        page.Scopes.Add(sc);
                }

                if (scope.IsUniqueOnEveryPage)
                {
                    foreach (var item in scope.UniqueItems)
                    {
                        Item translationItem = new Item() { ScopeID = sc.ID, IsMultiLineText = item.IsHtml, Key = item.Name };
                        translationItem.Pages.Add(page.ID);
                        AddTranslations(translationItem, page, item.Value);
                        Items.Add(translationItem);
                    }
                }

            }

            // Apply items (if they are related to this page!)

        }
        
        private void AddTranslations(Item item, Page p, string value)
        {
            item.Translations.Clear();

            if (!string.IsNullOrEmpty(value))
            {
                string val = value;
                if (val.Contains("${"))
                {
                    // Replace placeholders
                    val = val.Replace("${page_name}", p?.Name.Replace("_", " "));
                    val = val.Replace("${project_name}", Name.Replace("_", " "));
                    val = val.Replace("${project_url}", ProjectUrl);

                    // Variable
                    foreach (var lang in Languages)
                    {
                        var translation = new Translation() { Language = lang.LangCode, Text = val };

                        // ToDo: ** Tranlsate?

                        item.Translations.Add(translation);
                    }
                }
                else
                    item.Translations.Add(new Translation() { Language = MainLanguage, Text = value });
            }
        }

        #endregion

        public override string ToString()
        {
            return $"Project: {Name}";
        }
    }
}