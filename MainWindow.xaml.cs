using ControlzEx.Theming;
using Fluent;
using Translator.Controls;
using Translator.Controls.Dialogs;
using Translator.Controls.DropDownTemplates;
using Translator.Helper;
using Translator.Model;
using Translator.Model.Blog;
using Translator.Model.Json;
using Translator.Model.Log;
using Translator.Model.ViewModel;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Language = Translator.Model.Language;
using Microsoft.Web.WebView2.Core;
using Translator.Model.Tags;
using HtmlAgilityPack;

namespace Translator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
        #region Private Members
        private ScopeViewModel currentSelectedScope = null;
        private Language currentBlogLanguage;
        private BlogPreview blogPreviewWindow = null;
        private readonly List<string> alreadyCreatedFTPDirs = new List<string>();
        private ObservableCollection<PageViewModel> itemSource;

        #endregion

        #region Properties
        public static MainWindow W_INSTANCE { get; private set; }

        public PageViewModel CurrentSelectedPage { get; private set; } = null;

        public Language CurrentSelectedBlogLanguage
        {
            get => currentBlogLanguage;
            set
            {
                if (value != currentBlogLanguage)
                {
                    currentBlogLanguage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSelectedBlogLanguage)));
                    RefreshBlogs();
                }
            }
        }


        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Ctor
        public MainWindow()
        {
            W_INSTANCE = this;
            InitializeComponent();
            InitalizeLog();
            GeneralHelper.SearchForUpdates();
            MenuLanguages.DataContext = this;

            TextBlogContent.ApplyTheming();
            GeneralHelper.ApplyTheming();
            Project_OnProjectChanged();

            Loaded += MainWindow_Loaded;
            Project.OnProjectChanged += Project_OnProjectChanged;
            RecentlyOpenedProjects.RecentlyOpenedProjectsChanged += RecentlyOpenedProjects_RecentlyOpenedProjectsChanged;

            SetTagMode(false);
            MainGrid.RowDefinitions[MainGrid.RowDefinitions.Count - 1].Height = new GridLength(Settings.Instance.LogHeight, GridUnitType.Pixel);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            isInitalized = true;
            FileAssociations.FileAssociations.EnsureAssociationsSet();
            RefreshData();            
            RefreshTags();

            if (Project.CurrentProject != null && Project.CurrentProject.Tags.Count > 0)
                ListTags.SelectedIndex = 0;

            RecentlyOpenedProjects_RecentlyOpenedProjectsChanged();
            ClearInputValues();

            if (Consts.APP_VERSION == Consts.Version.Translator)
            {
                MenuButtonAdd.IsEnabled =
                ButtonGenerateJson.IsEnabled =
                MenuButtonSettings.IsEnabled =
                MenuOpenWorkspace.IsEnabled =
                MenuButtonLanguages.IsEnabled =
                MenuButtonUploadFTP.IsEnabled = false;

                // Disable right click menus on treeview
                var scopeChildContextMenu = TreeViewLangItems.FindResource("ScopeChildContextMenu") as System.Windows.Controls.ContextMenu;
                scopeChildContextMenu.Opacity = 0;
                scopeChildContextMenu.IsEnabled = false;

                // ItemMenu
                var itemMenu = TreeViewLangItems.FindResource("ItemMenu") as System.Windows.Controls.ContextMenu;
                itemMenu.Opacity = 0;
                itemMenu.IsEnabled = false;

                // PageChildContextMenu
                var pageChildContextMenu = TreeViewLangItems.FindResource("PageChildContextMenu") as System.Windows.Controls.ContextMenu;
                pageChildContextMenu.Opacity = 0;
                pageChildContextMenu.IsEnabled = false;

                // AddPageContextMenu
                var addPageContextMenu = GridMain.FindResource("AddPageContextMenu") as System.Windows.Controls.ContextMenu;
                addPageContextMenu.Opacity = 0;
                addPageContextMenu.IsEnabled = false;

                // Disable page menu
                ButtonMenuAddNewPage.IsEnabled =
                MenuButtonScopeAdd.IsEnabled =
                MenuButtonAdd.IsEnabled = false;

                string languages = string.Empty;
                int count = 0;
                foreach (var lang in Consts.SupportedLanguages)
                {
                    languages += lang.Name;

                    if (count < Consts.SupportedLanguages.Count - 1)
                        languages += ", ";

                    count++;
                }
                ShowStartMessage();
                return;
            }
            else
                ShowStartMessage();

            //MenuButtonTexterUpload.IsEnabled = false;

            // Ensure that old images are named corretly
            if (Project.CurrentProject != null)
            {
                Logger.LogInformation("Prüfe auf fehlerhafte Blog-Bilder ...", "Main");
                foreach (var item in Project.CurrentProject.BlogItems)
                {
                    foreach (var blog in item.Items)
                        blog.CopyAndFormatImage();
                }
            }
            Logger.LogInformation("Fertig!", "Main");

            var template = new TranslateBlogDropDownTemplate();
            template.OnStartedTranslation += Template_OnStartedTranslation;
            ButtonTranslateBlog.DropDown = template;

            var generateJsonTemplate = new GenerateJsonDropDownTemplate();
            generateJsonTemplate.OnJsonGeneration += GenerateJsonTemplate_OnJsonGeneration;
            ButtonGenerateJson.DropDown = generateJsonTemplate;

            SetBlogMode(BlogMode.View);
            SetTitle();
            closingMenuTimer.Tick += LoadingTimer_Tick;

            var webView2Envoirnment = await CoreWebView2Environment.CreateAsync(null, Consts.WebView2CachePath);
            await Browser.EnsureCoreWebView2Async(webView2Envoirnment);
        }

        private void ShowStartMessage()
        {
            Logger.LogInformation($"Translator (v{typeof(MainWindow).Assembly.GetName().Version.ToString(4)}) ist gestartet!", "Main");
        }

        #endregion

        #region Refresh Data / Blogs

        public void RefreshItemSource()
        {
            if (CurrentSelectedPage == null || currentSelectedScope == null)
                return;

            RefreshItemSource(CurrentSelectedPage, currentSelectedScope);
        }

        public void RefreshItemSource(PageViewModel parent, ScopeViewModel sp)
        {
            if (parent == null || sp == null)
            {
                if (parent == null)
                    Logger.LogDebug("Parent war null!", "Main");

                return;
            }

            currentSelectedScope = sp;
            MenuButtonAdd.IsEnabled = Consts.APP_VERSION == Consts.Version.Admin;

            RefreshItems(parent.PageBehind.GetItems(sp.ScopeBehind).ToArray());
        }

        public void RefreshItemSource(Item item)
        {
            if (item == null)
                return;

            RefreshItems(item);
        }

        private void RefreshItems(params Item[] items)
        {
            TranslationItems.Children.Clear();

            foreach (var item in items.OrderBy(p => p.Key))
            {
                TranslationItem translationItem = new TranslationItem();
                translationItem.Initalize(item, CurrentSelectedPage.PageBehind);

                TranslationItems.Children.Add(translationItem);
            }
        }

        public void RefreshData()
        {
            if (Project.CurrentProject == null)
                return;

            PageViewModel oldPage = CurrentSelectedPage;
            ScopeViewModel oldScope = currentSelectedScope;

            bool selectItemsWithMissingTranslations = ButtonSwitchDifferences.IsChecked.Value;
            itemSource = PageViewModel.BuildViewModel(itemSource, selectItemsWithMissingTranslations);
            TextPageCount.Text = itemSource.Count.ToString();

            TreeViewLangItems.ItemsSource = null;
            TreeViewLangItems.ItemsSource = itemSource;

            MenuLanguages.ItemsSource = Consts.APP_VERSION == Consts.Version.Admin ? Project.CurrentProject.Languages : Consts.SupportedLanguages;

            if (currentBlogLanguage == null)
                CurrentSelectedBlogLanguage = Project.CurrentProject.Languages.FirstOrDefault(p => p.LangCode == Project.CurrentProject.MainLanguage);

            RefreshItemSource(oldPage, oldScope);
        }

        public void RefreshTags()
        {
            if (Project.CurrentProject == null)
                return;

            ListTags.ItemsSource = Project.CurrentProject.Tags.OrderBy(t => t.Name).ToList();
            Tags.Refresh();
        }

        #endregion

        #region Menu Buttons

        private void MenuPageScopeAddClick_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectedPage != null)
            {
                ScopeAddDialog scopeAddDialog = new ScopeAddDialog(CurrentSelectedPage.PageBehind);
                var result = scopeAddDialog.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    Scope s = new Scope(scopeAddDialog.ResultName);

                    if (scopeAddDialog.Result.Count == 1 && scopeAddDialog.Result.First() == Page.GENERAL)
                        Project.CurrentProject.GeneralScopes.Add(s);
                    else
                    {
                        foreach (var page in scopeAddDialog.Result)
                            page.Scopes.Add(s);
                    }

                    Project.CurrentProject.Save();
                    RefreshData();
                }
            }
        }

        private void MenuAddPage_Click(object sender, RoutedEventArgs e)
        {
            AddNewPage();
        }

        private void ButtonMenuAddNewPage_Click(object sender, RoutedEventArgs e)
        {
            AddNewPage();
        }

        private void MenuPageRemovePageClick_Click(object sender, RoutedEventArgs e)
        {
            if (TreeViewLangItems.SelectedItem is PageViewModel p)
            {
                if (p.PageBehind == Page.GENERAL)
                {
                    MessageBox.Show($"Diese Seite kann nicht gelöscht werden!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show($"Möchten Sie die Seite \"{p.DisplayName}\" wirklich löschen?", "Seite wirklich löschen?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Project.CurrentProject.Pages.Remove(p.PageBehind);
                    p.PageBehind.ClearItems();
                    p.PageBehind.ClearScopesIfRequired();
                    Project.CurrentProject.Save();
                    RefreshData();
                }
            }
        }

        private void ButtonDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (TreeViewLangItems.SelectedItem is ItemViewModel i)
            {
                if (MessageBox.Show(this, $"Möchten Sie das Item \"{i.Key}\" wirklich löschen?", "Löschen?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Project.CurrentProject.Items.Remove(i.ItemBehind);
                    Project.CurrentProject.Save();
                    RefreshData();
                }
            }
        }

        private void MenuScopeRemove_Click(object sender, RoutedEventArgs e)
        {
            // Hint: Scope * (Displayname: general) cannot be removed!
            if (TreeViewLangItems.SelectedItem is ScopeViewModel sp)
            {
                if (sp.ScopeBehind == Scope.GENERAL)
                {
                    MessageBox.Show("Dieser Bereich darf nicht entfernt werden!", "Kann nicht gelöscht werden!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Determine scope "owner" and remove the scope just from that page
                if (CurrentSelectedPage != null)
                {
                    if (CurrentSelectedPage.PageBehind == Page.GENERAL)
                    {
                        if (MessageBox.Show($"Möchten Sie den Bereich \"{sp.DisplayName}\" wirklich überall löschen?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            Project.CurrentProject.GeneralScopes.Remove(sp.ScopeBehind);
                            sp.ScopeBehind.ClearItems(Page.GENERAL);
                            Project.CurrentProject.Save();
                            RefreshData();
                        }
                    }
                    else
                    {
                        if (MessageBox.Show($"Möchten Sie den Bereich \"{sp.DisplayName}\" aus der Seite \"{CurrentSelectedPage.DisplayName}\" wirklich löschen? Sollte dieser Bereich in anderen Seiten bereits vorkommen, wird er dort nicht gelöscht!", "Sind Sie sich sicher?", MessageBoxButton.YesNo, MessageBoxImage
                            .Question) == MessageBoxResult.Yes)
                        {
                            CurrentSelectedPage.PageBehind.Scopes.Remove(sp.ScopeBehind);
                            sp.ScopeBehind.ClearItems(CurrentSelectedPage.PageBehind);
                            Project.CurrentProject.Save();
                            RefreshData();
                        }
                    }
                }
            }
        }

        private void MenuButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            ShowItemDialog();
        }

        private void MenuItemAdd_Click(object sender, RoutedEventArgs e)
        {
            ShowItemDialog();
        }

        private void MenuButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsDialog().ShowDialog();
        }

        private void MenuButtonLanguages_Click(object sender, RoutedEventArgs e)
        {
            new ManageLanguagesDialog().ShowDialog();
        }

        private void MenutButtonEditScope_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuButtonEditPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectedPage != null && CurrentSelectedPage.PageBehind != Page.GENERAL)
            {
                new EditPageDialog(CurrentSelectedPage.PageBehind).ShowDialog();
                RefreshData();
            }
        }

        private void MenuOpenWorkspace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Project.CurrentProject.ProjectPath) && System.IO.Directory.Exists(Project.CurrentProject.ProjectPath))
                    System.Diagnostics.Process.Start("explorer", Project.CurrentProject.ProjectPath);
            }
            catch
            { }
        }

        private void MenuOpenAppPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Project.CurrentProject.PublicFolder) && System.IO.Directory.Exists(Project.CurrentProject.PublicFolder))
                    System.Diagnostics.Process.Start("explorer", Project.CurrentProject.PublicFolder);
            }
            catch
            { }
        }

        private void MenuButtonImport_Click(object sender, RoutedEventArgs e)
        {
           /* OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "XML Datendatei(*.xml)| *.xml" };
            bool? result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    Data instance = Serialization.Serialization.Read<Data>(openFileDialog.FileName, Serialization.Serialization.Mode.Normal);
                    if (instance == null)
                        throw new ArgumentNullException("Invalid data found");

                    OpenFileDialog ofd = new OpenFileDialog { Filter = "XML Blogs(*.xml)| *.xml" };
                    bool? result2 = ofd.ShowDialog();

                    Blogs blogs = Serialization.Serialization.Read<Blogs>(ofd.FileName, Serialization.Serialization.Mode.Normal);
                    if (blogs == null)
                        throw new ArgumentNullException("Invalid data found");
                   

                    //Project.CurrentProject.Merge(instance, null);
                    Project.CurrentProject.Save();

                    RefreshData();
                    RefreshBlogs();

                    MessageBox.Show("Fertig!", "Erfolg!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Lesen der Datendatei: {ex.Message}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }*/
        }

        private async void MenuButtonTexterUpload_Click(object sender, RoutedEventArgs e)
        {
            if (Consts.APP_VERSION == Consts.Version.Admin)
                return;

            // Okay, so upload to Data.en.XML to Language
            string fileName = $"Data.{Consts.SupportedLanguages.First().LangCode}.xml";
            bool result = await UploadToFtp(Project.CurrentProject.TexterFTPHost, Project.CurrentProject.ProjectFilePath, fileName, Project.CurrentProject.TexterFTPUser, Project.CurrentProject.TexterFTPPassword);
            if (result)
                MessageBox.Show("Fertig!", "Erfolg!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StackPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
                return;

            ButtonTranslateBlog.IsEnabled = false;
            CurrentSelectedBlogLanguage = (sender as System.Windows.Controls.StackPanel).DataContext as Language;
        }

        private void MenuButtonExit_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Template_OnStartedTranslation(List<Language> result, string sourceLanguage)
        {
            if (currentSelectedBlogItem == null)
                return;

            if (Project.CurrentProject.BlogItems.SelectMany(p => p.Items).Where(p => p.ParentID == currentSelectedBlogItem.ID).Any())
            {
                if (MessageBox.Show($"Der Blog \"{currentSelectedBlogItem.Title}\" wurde bereits übersetzt! Sind Sie sich sicher, dass Sie den Blog erneut übersetzen/aktualisieren möchten?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
            }

            await TranslateBlogsAsync(new BlogItem[] { currentSelectedBlogItem }, sourceLanguage, result.ToArray());
            bool containsATag = currentSelectedBlogItem.Text.Contains("<a href=");

            string text = "Fertig!";
            if (containsATag)
                text += " Achtung! Der Blog-Artikel enthält Links!";

            MessageBox.Show(text, "Fertig", MessageBoxButton.OK, (containsATag ? MessageBoxImage.Warning : MessageBoxImage.Information));
        }

        public async Task TranslateBlogsAsync(BlogItem[] items, string sourceLangauge, params Language[] languages)
        {
            MainDockPanel.IsEnabled = false;
            foreach (var currentSelectedBlogItem in items)
            {
                // Check if there is a blog translation already
                var blogID = currentSelectedBlogItem.ID;

                foreach (var lang in languages.Where(p => p.IsSelected))
                {
                    if (lang.LangCode == Project.CurrentProject.MainLanguage)
                        continue;

                    Logger.LogInformation($"Übersetzte Blog \"{currentSelectedBlogItem.Title}\" in die Sprache {lang.Name} ...", "BlogTranslate");

                    try
                    {
                        var blogs = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang.LangCode).FirstOrDefault();
                        if (blogs == null)
                        {
                            var assoc = new BlogItemLanguageAssoc() { Items = new System.Collections.ObjectModel.ObservableCollection<BlogItem>(), LangCode = lang.LangCode };
                            Project.CurrentProject.BlogItems.Add(assoc);
                            blogs = assoc;
                        }

                        var translatedBlogItem = await currentSelectedBlogItem.Translate(sourceLangauge, lang.LangCode);
                        var oldBlog = blogs.Items.Where(p => p.ParentID == currentSelectedBlogItem.ID).FirstOrDefault();
                        if (oldBlog != null)
                            blogs.Items.Remove(oldBlog);

                        blogs.Items.Add(translatedBlogItem);
                        Project.CurrentProject.Save();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Fehler beim Übersetzen des Blogs in der Sprache {lang.Name}: {ex.Message}! Sofern vorhanden bleibt der alte Blog bestehen!", "BlogTranslate");
                    }
                }
            }

            Logger.LogInformation("Fertig!", "BlogTranslate");
            MainDockPanel.IsEnabled = true;
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutDialog().ShowDialog();
        }

        #endregion

        #region Generate JSON
        public JsonPage GeneratePageJson(Page page, Language language)
        {
            var jsonPage = new JsonPage();

            foreach (var scope in page.ScopesAll)
            {
                if (page.ScopesAll.Contains(scope))
                {
                    string scopeName = scope.DisplayName.Replace(" (*)", string.Empty);

                    if (!Project.CurrentProject.PagesAll.Any(x => x != page && x.ScopesAll.Contains(scope)))
                        jsonPage.Page.Add(scopeName, new JObject());
                    else if (scope != Scope.GENERAL)
                        jsonPage.General.Add(scopeName, new JObject());
                }
            }

            foreach (var item in page.GetItems())
            {
                // Determine if item should be added in "page" or in "general"
                string scopeName = item.Scope.DisplayName;
                object toAdd = (object)item.Translate(language, true);

                if (item.DPages.Contains(page) && item.Pages.Count <= 1)
                {
                    if (item.Scope == Scope.GENERAL)
                        jsonPage.Page.Add(new JProperty(item.Key, toAdd));
                    else
                        (jsonPage.Page[scopeName] as JObject).Add(new JProperty(item.Key, toAdd));
                }
                else
                {
                    if (item.Scope == Scope.GENERAL)
                        jsonPage.General.Add(new JProperty(item.Key, toAdd));
                    else
                        (jsonPage.General[scopeName] as JObject).Add(new JProperty(item.Key, toAdd));
                }
            }

            // Search for empty entries and remove them
            List<JToken> toRemove = new List<JToken>();
            foreach (var entry in jsonPage.Page)
            {
                if (entry.Value is JObject j)
                {
                    if (j.Count == 0)
                        toRemove.Add(entry.Value);
                }
            }

            foreach (var item in toRemove)
                jsonPage.Page.Remove((item.Parent as JProperty).Name);


            toRemove.Clear();
            foreach (var entry in jsonPage.General)
            {
                if (entry.Value is JObject j)
                {
                    if (j.Count == 0)
                        toRemove.Add(entry.Value);
                }
            }

            foreach (var item in toRemove)
                jsonPage.General.Remove((item.Parent as JProperty).Name);

            // Set them to null if they don't have any items!
            if (jsonPage.General.Count == 0)
                jsonPage.General = null;
            if (jsonPage.Page.Count == 0)
                jsonPage.Page = null;

            return jsonPage;
        }

        private async void MenuGenerateJson_Click(object sender, RoutedEventArgs e)
        {
            await StartJsonGeneration(JsonGeneration.All);
        }

        private async void GenerateJsonTemplate_OnJsonGeneration(JsonGeneration mode)
        {
            await StartJsonGeneration(mode);
        }

        private async Task StartJsonGeneration(JsonGeneration mode)
        {
            bool success = false;
            MainDockPanel.IsEnabled = false;
            if (!System.IO.Directory.Exists(Project.CurrentProject.LocalizationsFolder))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(Project.CurrentProject.LocalizationsFolder);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Fehler beim Erstellen des Ordners \"{Project.CurrentProject.LocalizationsFolder}\": {ex.Message}", "JsonExport");
                }
            }

            success = await Task.Run(new Func<bool>(() => Generate(Project.CurrentProject.LocalizationsFolder, mode)));
            MainDockPanel.IsEnabled = true;
            if (success)
                MessageBox.Show("Fertig!", "Erfolg!", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private bool Generate(string path, JsonGeneration mode)
        {
            Logger.LogInformation($"Starte JSON Export ({mode}) ...", "JsonExport");

            // OK, so we have to create two folders if they don't exists already:
            // "php" and "js"
            // Then we need to iterate each page and generate e.g. page.php.de.json and page.js.de.json
            string phpDirectory = System.IO.Path.Combine(path, "php");
            string blogsDirectory = System.IO.Path.Combine(path, "blogs");
            string tagsDirectory = System.IO.Path.Combine(path, "tags");

            // Step 1) Clear workspace
            try
            {
                if (System.IO.Directory.Exists(phpDirectory) && (mode == JsonGeneration.All || mode == JsonGeneration.OnlyLang))
                    System.IO.Directory.Delete(phpDirectory, true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fehler beim Erstellen von \"{phpDirectory}\": {ex.Message}", "JsonExport");
            }

            // Still Step 1)
            try
            {
                if (System.IO.Directory.Exists(blogsDirectory) && (mode == JsonGeneration.All || mode == JsonGeneration.OnlyBlogs))
                    System.IO.Directory.Delete(blogsDirectory, true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fehler beim Aufräumen von \"{blogsDirectory}\": {ex.Message}", "JsonExport");
            }


            // Clean up preview files
            try
            {
                System.IO.File.Delete(System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, Consts.IndexFile));
                System.IO.File.Delete(System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, Consts.IndexDarkFile));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fehler beim Aufräumen von der Assets: {ex.Message}", "JsonExport");
            }


            // Step 2) Create php directory
            try
            {
                System.IO.Directory.CreateDirectory(phpDirectory);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fehler beim Erstellen von \"{phpDirectory}\": {ex.Message}", "JsonExport");
            }

            // Step 3) Generate Page Json + Blogs Json Files
            try
            {
                if (mode == JsonGeneration.All || mode == JsonGeneration.OnlyLang)
                {
                    foreach (var page in Project.CurrentProject.Pages)
                    {
                        // Special case for the blog page (blog article page)
                        if (page.Name == "blog")
                            continue;

                        foreach (var lang in Project.CurrentProject.Languages)
                        {
                            Logger.LogInformation($"Generiere Seite \"{page.Name}\" für die Sprache \"{lang.Name}\" ...", "JsonExport");
                          
                            var jsonPage = GeneratePageJson(page, lang);

                           /* string[] pagesWithBlogPreview = new string[] { "index", "user", "zoom", "share", "blogs", "faq" };
                            if (pagesWithBlogPreview.Contains(page.Name))
                            {
                                // Generate and add blog previews first
                                jsonPage.BlogPreviews = GenerateBlogsPreview(lang);
                            }*/

                            try
                            {
                                System.IO.File.WriteAllText(System.IO.Path.Combine(phpDirectory, $"{page.DisplayName}.{lang.LangCode}.json"), JsonConvert.SerializeObject(jsonPage, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"Fehler beim Speichern der Seite \"{page.DisplayName}.{lang.LangCode}.json\". Fehler: {ex.Message}", "JsonExport");
                            }
                        }
                    }

                    // Step  4) Generate tags

                    // Step 4.1) Clean UP
                    try
                    {
                        if (System.IO.Directory.Exists(tagsDirectory))
                            System.IO.Directory.Delete(tagsDirectory, true);

                        if (System.IO.Directory.Exists(Project.CurrentProject.TagImagesFolder))
                            System.IO.Directory.Delete(Project.CurrentProject.TagImagesFolder, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Fehler beim Aufräumen von \"{tagsDirectory}\": {ex.Message}", "JsonExport");
                    }

                    // Step 4.2) Create tags directory and Project.CurrentProject.TagImagesFolder directory
                    try
                    {
                        System.IO.Directory.CreateDirectory(tagsDirectory);
                        System.IO.Directory.CreateDirectory(Project.CurrentProject.TagImagesFolder);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Fehler beim Erstellen von \"{tagsDirectory}\": {ex.Message}", "JsonExport");
                    }


                    foreach (var tag in Project.CurrentProject.Tags)
                    {
                        foreach (var lang in Project.CurrentProject.Languages)
                        {
                            List<JObject> relatedBlogs = new List<JObject>();

                            // Determine all blogs which are related to this tag
                            var assoc = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang.LangCode).FirstOrDefault();
                            if (assoc != null)
                            {
                                var relatedBlogItems = assoc.Items.Where(p => p.Tags.Contains(tag.ID));
                                foreach (var blg in relatedBlogItems)
                                    relatedBlogs.Add(BlogItemPreview.FromBlogItem(blg, lang, true).ToJObject(true));
                            }

                            string title = tag.NameTranslations.Translate(lang.LangCode);
                            string imageFile = title.CreateUrlTitle();

                            // Create images
                            string currentFilePath = System.IO.Path.Combine(Project.CurrentProject.RawTagFolder, tag.ID + ".png");

                            try
                            {
                                System.IO.File.Copy(currentFilePath, System.IO.Path.Combine(Project.CurrentProject.TagImagesFolder, imageFile + ".png"));
                                ImageHelper.CreateWebPImage(currentFilePath, System.IO.Path.Combine(Project.CurrentProject.TagImagesFolder, imageFile + Consts.WEBP));
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"Fehler beim Kopieren des Tag-Bildes: {ex.Message}", "TagExport");
                            }

                            // Create the tag
                            JObject jTag = new JObject
                            {
                                ["title"] = title,
                                ["description"] = tag.DescriptionTranslations.Translate(lang.LangCode),
                                ["related_blogs"] = new JArray(relatedBlogs),
                                ["images"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["format"] = "png",
                                        ["name"] = imageFile + ".png",
                                    },
                                    new JObject
                                    {
                                        ["format"] = "webp",
                                        ["name"] = imageFile + Consts.WEBP,
                                    }
                                },
                            };
                            try
                            {
                                string json = JsonConvert.SerializeObject(jTag, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None });
                                System.IO.File.WriteAllText(System.IO.Path.Combine(tagsDirectory, $"{tag.Name}.{lang.LangCode}.json"), json);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"Fehler beim Speichern des Tags \"{tag.Name}.{lang.LangCode}.json\". Fehler: {ex.Message}", "TagExport");
                            }

                        }
                    }

                }

                if (mode == JsonGeneration.All || mode == JsonGeneration.OnlyBlogs)
                    GenerateBlogsJSON(path);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fehler beim Generieren der JSON-Dateien: {ex.Message}", "JsonExport");
                return false;
            }
        }

        public List<BlogItemPreview> GenerateBlogsPreview(Language lang)
        {
            var blogItems = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang.LangCode).FirstOrDefault();
            if (blogItems == null)
                return null;

            // Generate preview: /localizations/blogs/preview
            // Generate items:   /localizations/items/de
            // Generate items:   /localizations/items/en ...
            // Images: /img/content/image-name.jpg
            List<BlogItemPreview> previewBlogs = new List<BlogItemPreview>();

            foreach (var blog in blogItems.Items.OrderByDescending(p => p.PublishedDate))
            {
                blog.UrlName = blog.UrlName.CreateUrlTitle();
                var preview = BlogItemPreview.FromBlogItem(blog, lang);
                previewBlogs.Add(preview);
            }

            return previewBlogs;
        }

        public void GenerateBlogsJSON(string path)
        {
            // Create folders
            Logger.LogInformation($"Generiere Blogs ...", "BlogExport");
            string[] folders = new string[] { System.IO.Path.Combine(path, "blogs"), System.IO.Path.Combine(path, "blogs", "preview"), System.IO.Path.Combine(path, "blogs", "items") };

            foreach (var folder in folders)
            {
                try
                {
                    if (System.IO.Directory.Exists(folder))
                        System.IO.Directory.Delete(folder);

                    if (!System.IO.Directory.Exists(folder))
                        System.IO.Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Fehler beim Erstellen des Ordners: {ex.Message}. Fehler: {ex.Message}", "BlogExport");
                }
            }

            // Generate blogs
            foreach (var lang in Project.CurrentProject.Languages)
            {
                var blogItems = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang.LangCode).FirstOrDefault();
                if (blogItems == null)
                {
                    // System.IO.File.WriteAllText(System.IO.Path.Combine(path, "blogs", "preview", $"preview.{lang.LangCode}.json"), "[]");
                    Logger.LogInformation($"Für die Sprache \"{lang.Name}\" gibt es keine Blogs!", "BlogExport");
                    continue;
                }

                // Generate preview: /localizations/blogs/preview
                // Generate items:   /localizations/items/de
                // Generate items:   /localizations/items/en ...
                // Images: /img/content/image-name.jpg

                string previewPath = System.IO.Path.Combine(path, "blogs", "preview");
                List<BlogItemPreview> previewBlogs = new List<BlogItemPreview>();

                foreach (var blog in blogItems.Items.OrderByDescending(p => p.PublishedDate))
                {
                    Logger.LogInformation($"Generiere Blog \"{blog.Title}\" ...", "BlogExport");

                    blog.UrlName = blog.UrlName.CreateUrlTitle();

                    // 1) Save preview
                    var preview = BlogItemPreview.FromBlogItem(blog, lang);
                    previewBlogs.Add(preview);

                    // 2) Save items
                    string itemPath = System.IO.Path.Combine(path, "blogs", "items", lang.LangCode);
                    if (!System.IO.Directory.Exists(itemPath))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(itemPath);
                        }
                        catch
                        {

                        }
                    }

                    try
                    {
                        string finalItemPath = System.IO.Path.Combine(itemPath, $"{blog.UrlName}.json");

                        var jsonPage = GeneratePageJson(Project.CurrentProject.Pages.Where(p => p.Name == "blog").FirstOrDefault(), lang);

                        /* 
                         * page.google.description
                           page.google.name
                           page.google.image

                           page.facebook.description
                           page.facebook.name
                           page.facebook.url
                           page.facebook.image 
                        */
                        // ToDo: *** /img/blog/ (User should be able to decide if this is necessary and must set the blog image url)
                        string fullImageUrl = $"{Project.CurrentProject.ProjectUrl}/img/blog/{blog.ImageFileName}";
                        if (jsonPage.Page == null)
                            jsonPage.Page = new JObject();

                        if (!jsonPage.Page.ContainsKey("google"))
                            jsonPage.Page.Add("google", new JObject());
                        if (!jsonPage.Page.ContainsKey("facebook"))
                            jsonPage.Page.Add("facebook", new JObject());

                        var google = jsonPage.Page["google"];
                        google["description"] = blog.MetaInfo.Description.StripInvalidMetaChars();
                        google["name"] = blog.Title;
                        google["image"] = fullImageUrl;

                        var facebook = jsonPage.Page["facebook"];
                        facebook["description"] = blog.MetaInfo.Description.StripInvalidMetaChars();
                        facebook["name"] = blog.Title;
                        facebook["image"] = fullImageUrl;
                        facebook["url"] = blog.GenerateLink(lang.LangCode);
                        facebook["type"] = "article";
                        facebook["title"] = blog.Title;

                        var jBlog = blog.ToJObject(lang);

                        foreach (var property in jBlog)
                        {
                            // If exists, overwrite it with blog-specific content (user cannnot add all blog-meta info in a single page)
                            if (!jsonPage.Page.ContainsKey(property.Key))
                                jsonPage.Page.Add(new JProperty(property.Key, property.Value));
                            else
                                jsonPage.Page[property.Key] = property.Value;
                        }

                        // jsonPage.BlogPreviews = GenerateBlogsPreview(lang);
                        string json = JsonConvert.SerializeObject(jsonPage, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None });

                        System.IO.File.WriteAllText(finalItemPath, json, new System.Text.UTF8Encoding(false));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Fehler beim Exportieren des Blogs: \"{blog.Title}\". Fehler: {ex.Message}", "BlogExport");
                    }
                }

                try
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(previewPath, $"preview.{lang.LangCode}.json"), JsonConvert.SerializeObject(previewBlogs));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Fehler beim Speichern der Vorschau \"preview.{lang.LangCode}.json\". Fehler: {ex.Message}", "BlogExport");
                }
            }
        }

        #endregion

        #region Upload To FTP

        private async void MenuButtonUploadFTP_Click(object sender, RoutedEventArgs e)
        {
            MainDockPanel.IsEnabled = false;
            alreadyCreatedFTPDirs.Clear();

            // Preparing to generate json files in a temp-dir
            string path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "temp");

            // Generate json files
            bool res = await Task.Run(new Func<bool>(() => Generate(path, JsonGeneration.All)));
            if (!res)
                return;

            // Try to upload these files via ftp
            string[] files = System.IO.Directory.GetFiles(path, "*.*", System.IO.SearchOption.AllDirectories);
            bool success = true;

            int currentCounter = 1;
            foreach (string file in files)
            {
                string fileName = file.Replace($@"{path}\", string.Empty).Replace(@"\", "/");
                int percentage = (int)Math.Round((currentCounter / (double)files.Length) * 100, 2);
                Logger.LogInformation($"Lade Datei \"{fileName}\" hoch ({percentage}%) ...", "FTPUpload");

                bool result = await UploadToFtp(Project.CurrentProject.TexterFTPHost, file, $"/localizations/{fileName}", Project.CurrentProject.TexterFTPUser, Project.CurrentProject.TexterFTPPassword.Decrypt());
                if (!result)
                {
                    success = false;
                    break;
                }

                currentCounter++;
            }

            // Upload blog images
            string[] imageFiles = System.IO.Directory.GetFiles(Project.CurrentProject.BlogImagesFolder);
            currentCounter = 0;
            foreach (var imageFile in imageFiles)
            {
                // Skip html preview files (if any)
                if (imageFile.EndsWith(".html"))
                    continue;

                int percentage = (int)Math.Round((currentCounter / (double)files.Length) * 100, 2);
                Logger.LogInformation($"Lade Bild {imageFile} hoch ({percentage}%) ...", "FTPUpload");
                await UploadToFtp(Project.CurrentProject.TexterFTPHost, imageFile, $"/img/content/{System.IO.Path.GetFileName(imageFile)}", Project.CurrentProject.TexterFTPUser, Project.CurrentProject.TexterFTPPassword.Decrypt());
            }

            if (success && files.Length > 0)
                MessageBox.Show(this, "Fertig!", "Erfolg!", MessageBoxButton.OK, MessageBoxImage.Information);

            // Clean up
            try
            {
                System.IO.Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fehler beim Aufräumen von {path}. Fehler: {ex.Message}", "FTPUpload");
            }

            MainDockPanel.IsEnabled = true;
        }

        private async Task<bool> UploadToFtp(string host, string localSourceFile, string ftpSubPath, string username, string password)
        {
            try
            {
                await Task.Run(() =>
                {
                    CreateFolder(Project.CurrentProject.TexterFTPHost, $"{System.IO.Path.GetDirectoryName(ftpSubPath).Replace(@"\", "/")}", Project.CurrentProject.TexterFTPUser, Project.CurrentProject.TexterFTPPassword.Decrypt());
                });

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{host}/{ftpSubPath}");
                request.Credentials = new NetworkCredential(username, password);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                using (System.IO.Stream fileStream = System.IO.File.OpenRead(localSourceFile))
                using (System.IO.Stream ftpStream = await request.GetRequestStreamAsync())
                {
                    byte[] buffer = new byte[10240];
                    int read;
                    while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await ftpStream.WriteAsync(buffer, 0, read);
                        Logger.LogInformation($"Uploaded {fileStream.Position} bytes ... ", "FTPUploadFile");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fehler beim Hochladen der Datei: {ftpSubPath}{Environment.NewLine}{Environment.NewLine}{ex.Message}", "FTPUploadFile");
                return false;
            }
        }

        public void CreateFolder(string ftpAddress, string pathToCreate, string login, string password)
        {
            string[] subDirs = pathToCreate.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

            string currentDir = string.Format("ftp://{0}", ftpAddress);

            foreach (string subDir in subDirs)
            {
                currentDir = currentDir + "/" + subDir;
                if (alreadyCreatedFTPDirs.Contains(currentDir))
                    continue;

                try
                {
                    FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(currentDir);
                    reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;
                    reqFTP.UseBinary = true;
                    reqFTP.Credentials = new NetworkCredential(login, password);
                    FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                    System.IO.Stream ftpStream = response.GetResponseStream();
                    ftpStream.Close();
                    response.Close();

                    alreadyCreatedFTPDirs.Add(currentDir);
                }
                catch (Exception)
                {
                    // directory already exist I know that is weak but there is no way to check if a folder exist on ftp...
                    alreadyCreatedFTPDirs.Add(currentDir);
                }
            }
        }

        #endregion

        #region Blogs

        private BlogItem currentSelectedBlogItem = null;
        private bool newImageAdded = false;
        private string imagePath = string.Empty;
        private BlogMode currentBlogMode = BlogMode.View;

        public void RefreshBlogs()
        {
            var lang = CurrentSelectedBlogLanguage;
            TextBlogContent.RefreshBlogs(lang);
            var items = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang?.LangCode).FirstOrDefault();

            if (items != null)
            {
                var source = items.Items.OrderBy(p => p.PublishedDate).ToList();
                int oldSelectedIndex = ListBlogs.SelectedIndex;
                ListBlogs.Items.Clear();

                foreach (var blogItem in source)
                {
                    var entry = new BlogViewItem();
                    entry.SetItem(blogItem);
                    blogItem.Language = lang.LangCode;

                    ListBlogs.Items.Add(new System.Windows.Controls.Viewbox() { Child = entry });
                }

                if (oldSelectedIndex != -1 && oldSelectedIndex < ListBlogs.Items.Count)
                    ListBlogs.SelectedIndex = oldSelectedIndex;
                return;
            }

            ListBlogs.ItemsSource = null;
            ListBlogs.Items.Clear();
            ButtonTranslateBlog.IsEnabled = false;
            ClearInputValues();
        }

        private void ButtonAddBlog_Click(object sender, RoutedEventArgs e)
        {
            SetBlogMode(BlogMode.Add);
        }

        private void ButtonDeleteBlog_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedBlogItem != null)
            {
                // und alle zugehörigen übersetzen Blogs 
                if (MessageBox.Show($"Sind Sie sich wirklich sicher, dass Sie den Blog \"{currentSelectedBlogItem.Title}\" wirklich löschen möchten?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var lang = CurrentSelectedBlogLanguage;
                    var items = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang.LangCode).FirstOrDefault();

                    if (items != null)
                    {
                        // Delete blog item
                        items.Items.Remove(currentSelectedBlogItem);

                        // Associative blogs
                        /*foreach (var blg in Project.CurrentProject.BlogItems)
                        {
                            List<BlogItem> toDelete = blg.Items.Where(p => p.ParentID == currentSelectedBlogItem.ParentID).ToList();
                            foreach (var b in toDelete)
                                items.Items.Remove(b);
                        }*/

                        // Try also to delete image file
                        currentSelectedBlogItem.DeleteBlogImages();

                        Project.CurrentProject.Save();
                        Project.CurrentProject.Save();
                        RefreshBlogs();
                        ClearInputValues();
                    }
                }
            }
        }

        private void ClearInputValues()
        {
            // (TextUrlName will be cleard automatically when TextTitle will be cleared!)
            if (CheckOwnUrlName.IsChecked.Value)
                TextUrlName.Clear();
     
            TextBlogAltText.Clear();
            TextBlogContent.Clear();
            TextBlogMetaDescription.Clear();
            Keywords.Clear();
            TextBlogPreview.Clear();
            TextBlogPublisherName.Clear();
            TextBlogID.Clear();
            TextBlogTitle.Clear();
            DateBlogPublished.SelectedDate = (currentBlogMode == BlogMode.Add ? DateTime.Now : (DateTime?)null);
            CheckOwnUrlName.IsChecked = false;
            ClearImage();

            if (Project.CurrentProject != null)
                TextBlogPublisherName.Text = Project.CurrentProject.ProjectUrl.Replace("http://", string.Empty).Replace("https://", string.Empty);
        }

        private void ClearImage()
        {
            BlogImage.Clear();
        }

        public enum BlogMode
        {
            View,
            Edit,
            Add
        }

        private void SetBlogMode(BlogMode mode)
        {
            if (Project.CurrentProject == null)
                return;

            currentBlogMode = mode;
            if (mode == BlogMode.Add)
                ClearInputValues();

            ButtonEditBlog.IsEnabled = (mode == BlogMode.View) && currentSelectedBlogItem != null;
            ButtonAddBlog.IsEnabled = (mode == BlogMode.View);
            ButtonDeleteBlog.IsEnabled = (mode == BlogMode.View) && currentSelectedBlogItem != null;
            ListBlogs.IsEnabled = (mode == BlogMode.View);
            SetEditPanel(mode != BlogMode.View);
            MenuLanguages.IsEnabled = (mode == BlogMode.View);
            ButtonTranslateBlog.IsEnabled = (mode == BlogMode.View && CurrentSelectedBlogLanguage?.LangCode == Project.CurrentProject.MainLanguage); // enable only for german blogs
            ButtonEditOrAddBlog.IsEnabled = (mode == BlogMode.Edit || mode == BlogMode.Add);

            if (mode == BlogMode.Add)
                TextBlogID.Text = Guid.NewGuid().ToString();

            if (mode == BlogMode.Edit)
                TabControlMain.SelectedIndex = 1;
        }

        private void SetEditPanel(bool state)
        {
            ButtonTranslateBlog.IsEnabled = !state;
            ButtonEditOrAddBlog.IsEnabled = state;
            ButtonEditBlog.IsEnabled = !state && currentSelectedBlogItem != null;
            PanelProperties.IsEnabled = state;
            TextBlogContent.IsReadOnly = !state;
        }

        private void ButtonCheckForInvalidUrls_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedBlogItem == null)
                return;

            if (!BlogItem.ContainsInvalidUrls(TextBlogContent.Text, out string report))
                MessageBox.Show(this, report, "URL-Fehler gefunden!", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(this, "Es wurden keine Fehler gefunden!", "Gute Arbeit!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ButtonEditOrAddBlog_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectedBlogLanguage == null)
                return;

            var lang = CurrentSelectedBlogLanguage;

            if (currentBlogMode == BlogMode.Add)
            {
                // ToDo: Verify values must not be empty especially title!!!!
                if (string.IsNullOrEmpty(TextBlogTitle.Text))
                {
                    MessageBox.Show("Der Titel darf nicht leer sein!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (Project.CurrentProject.BlogItems.Any(x => x.LangCode == lang.LangCode))
                {
                    var items = Project.CurrentProject.BlogItems.FirstOrDefault(x => x.LangCode == lang.LangCode);
                    if (items != null && items.Items.Any(x => x.Title == TextBlogTitle.Text))
                    {
                        MessageBox.Show("Der Titel existiert bereits in dieser Sprache!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                DateTime date = DateTime.MinValue;
                if (DateBlogPublished.SelectedDate.HasValue)
                    date = DateBlogPublished.SelectedDate.Value;

                string urlName = TextBlogTitle.Text.CreateUrlTitle();
                if (CheckOwnUrlName.IsChecked.Value)
                    urlName = TextUrlName.Text.CreateUrlTitle();

                BlogItem blogItem = new BlogItem()
                {
                    AltText = TextBlogAltText.Text,
                    PreviewText = TextBlogPreview.Text,
                    Title = TextBlogTitle.Text,
                    Text = TextBlogContent.Text,
                    PublishedDate = date,
                    ModifiedDate = date,
                    Publisher = TextBlogPublisherName.Text,
                    ID = new Guid(TextBlogID.Text),
                    UrlName = urlName,
                    HasCustomUrl = CheckOwnUrlName.IsChecked.Value,
                    Tags = Tags.GetResult().Select(p => p.ID).ToList(),
                    MetaInfo = new BlogMeta()
                    {
                        Description = TextBlogMetaDescription.Text,
                        Keywords = Keywords.GetResult()
                    }
                };

                if (!blogItem.ContainsInvalidUrls(out string report))
                {
                    MessageBox.Show(this, report, "URL-Fehler gefunden!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                blogItem.CopyAndFormatImage(newImageFilePath: imagePath);

                BlogItemLanguageAssoc assoc = null;
                if (Project.CurrentProject.BlogItems.Any(x => x.LangCode == lang.LangCode))
                    assoc = Project.CurrentProject.BlogItems.Where(x => x.LangCode == lang.LangCode).First();

                if (assoc == null)
                {
                    assoc = new BlogItemLanguageAssoc() { LangCode = lang.LangCode };
                    Project.CurrentProject.BlogItems.Add(assoc);
                }

                assoc.Items.Add(blogItem);
                Project.CurrentProject.Save();
                RefreshBlogs();
            }
            else if (currentBlogMode == BlogMode.Edit)
            {
                if (!BlogItem.ContainsInvalidUrls(TextBlogContent.Text, out string report))
                {
                    MessageBox.Show(this, report, "URL-Fehler gefunden!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                currentSelectedBlogItem.AltText = TextBlogAltText.Text;
                currentSelectedBlogItem.PreviewText = TextBlogPreview.Text;
                currentSelectedBlogItem.Title = TextBlogTitle.Text;
                currentSelectedBlogItem.Text = TextBlogContent.Text;
                currentSelectedBlogItem.PublishedDate = DateBlogPublished.SelectedDate.Value;
                currentSelectedBlogItem.Publisher = TextBlogPublisherName.Text;
                currentSelectedBlogItem.ModifiedDate = DateTime.Now;
                currentSelectedBlogItem.HasCustomUrl = CheckOwnUrlName.IsChecked.Value;
                if (currentSelectedBlogItem.HasCustomUrl)
                    currentSelectedBlogItem.UrlName = TextUrlName.Text.CreateUrlTitle();
                else
                    currentSelectedBlogItem.UrlName = TextBlogTitle.Text.CreateUrlTitle();

                currentSelectedBlogItem.Tags = Tags.GetResult().Select(p => p.ID).ToList();

                currentSelectedBlogItem.MetaInfo = new BlogMeta()
                {
                    Description = TextBlogMetaDescription.Text,
                    Keywords = Keywords.GetResult()
                };

                if (newImageAdded)
                {
                    newImageAdded = false;
                    currentSelectedBlogItem.CopyAndFormatImage(newImageFilePath: imagePath);
                }
                else // ensure that the name changes when the UrlName changes
                    currentSelectedBlogItem.CopyAndFormatImage();

                Project.CurrentProject.Save();
                RefreshBlogs();
            }

            SetBlogMode(BlogMode.View);
        }

        private void TextBlogTitle_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Set alt text (if necessary)
            if (string.IsNullOrEmpty(TextBlogAltText.Text) || TextUrlName.Text == TextBlogAltText.Text)
                TextBlogAltText.Text = TextBlogTitle.Text.CreateUrlTitle();

            // Set url name
            if (!CheckOwnUrlName.IsChecked.Value)
                TextUrlName.Text = TextBlogTitle.Text.CreateUrlTitle();
        }

        private async void ListBlogs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SetEditPanel(false);
            if (ListBlogs.SelectedIndex == -1 || CurrentSelectedBlogLanguage == null)
            {
                ButtonEditBlog.IsEnabled = false;
                ButtonDeleteBlog.IsEnabled = false;
                ButtonTranslateBlog.IsEnabled = false;
                currentSelectedBlogItem = null;
                return;
            }
            else
                ButtonEditBlog.IsEnabled = true;

            var lang = CurrentSelectedBlogLanguage;

            ButtonTranslateBlog.IsEnabled = (lang.LangCode == Project.CurrentProject.MainLanguage);

            currentSelectedBlogItem = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang.LangCode).Select(p => p.Items.OrderBy(f => f.PublishedDate).ToList()[ListBlogs.SelectedIndex]).FirstOrDefault();
            SetBlogMode(BlogMode.View);

            // Set selection
            foreach (var assoc in Project.CurrentProject.BlogItems)
            {
                foreach (var item in assoc.Items)
                    item.IsSelected = false;
            }
            currentSelectedBlogItem.IsSelected = true;

            TextBlogTitle.Text = currentSelectedBlogItem.Title;
            if (currentSelectedBlogItem.HasCustomUrl)
            {
                TextUrlName.Text = currentSelectedBlogItem.UrlName;
                CheckOwnUrlName.IsChecked = true;
            }
            else
            {
                CheckOwnUrlName.IsChecked = false;
                TextUrlName.Text = currentSelectedBlogItem.Title.CreateUrlTitle();
            }

            TextBlogPreview.Text = currentSelectedBlogItem.PreviewText;
            TextBlogAltText.Text = currentSelectedBlogItem.AltText;
            TextBlogContent.Text = currentSelectedBlogItem.Text;

            TextBlogID.Text = currentSelectedBlogItem.ID.ToString();
            DateBlogPublished.SelectedDate = currentSelectedBlogItem.PublishedDate;
            TextBlogPublisherName.Text = currentSelectedBlogItem.Publisher;
            TextBlogMetaDescription.Text = currentSelectedBlogItem.MetaInfo?.Description;

            Tags.SetItems(Project.CurrentProject.Tags.Where(t => currentSelectedBlogItem.Tags.Contains(t.ID)).ToArray());
            Keywords.SetItems(currentSelectedBlogItem.MetaInfo?.Keywords);

            // Load image
            if (System.IO.Directory.Exists(Project.CurrentProject.BlogImagesFolder) && !string.IsNullOrEmpty(currentSelectedBlogItem.ImageFileName))
            {
                imagePath = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, currentSelectedBlogItem.ImageFileName);
                if (System.IO.File.Exists(imagePath))
                    BlogImage.LoadImage(imagePath);
                else
                    ClearImage();
            }
            else
                ClearImage();

            await RefreshHTMLPreview();
        }

        private void ButtonEditBlog_Click(object sender, RoutedEventArgs e)
        {
            if (ListBlogs.SelectedIndex == -1 || CurrentSelectedBlogLanguage == null)
                return;

            SetBlogMode(BlogMode.Edit);
        }

        private void AddImageControl_ImageAdded(object sender, string e)
        {
            if (currentSelectedBlogItem != null)
                newImageAdded = true;

            imagePath = e;
        }

        #endregion

        #region Tags

        private bool isInEditMode = false;
        private Tag currentSelectedTag = null;

        public void SetTagMode(bool state)
        {
            if (!state)
            {
                TextTagElementName.IsEnabled = false;
                ImageControlTag.IsEnabled = false;
                ButtonCreateTag.IsEnabled = true;
                ButtonRemoveTag.IsEnabled = true;
                ListTags.IsEnabled = true;

                ButtonEditTag.Header = "Bearbeiten";
                TranslationTagName.SetReadOnly();
                TranslationTagDescription.SetReadOnly();
            }
            else
            {
                TextTagElementName.IsEnabled = true;
                ImageControlTag.IsEnabled = true;
                ButtonCreateTag.IsEnabled = false;
                ButtonRemoveTag.IsEnabled = false;

                ButtonEditTag.Header = "Bearbeiten beenden";
                ListTags.IsEnabled = false;
                TranslationTagName.SetWriteable();
                TranslationTagDescription.SetWriteable();
            }
        }

        private async void ButtonCreateTag_Click(object sender, RoutedEventArgs e)
        {
            var result = InputDialog.ShowInputDialog("Tag erstellen", "Bitte geben Sie einen Namen für den Tag ein!");

            if (!string.IsNullOrEmpty(result))
            {
                var tag = new Tag() { Name = result };
                tag.NameTranslations = new Item() { Key = "name" };
                tag.DescriptionTranslations = new Item() { Key = "description", IsMultiLineText = true };

                // Prepare translations automatically
                foreach (var lang in Project.CurrentProject.Languages)
                {
                    if (lang.LangCode != Project.CurrentProject.MainLanguage)
                    {
                        var translationResult = await TranslationHelper.TranslateText(result, Project.CurrentProject.MainLanguage, lang.LangCode, TranslationAPI.OnlyGoogleFreeTranslation);

                        if (translationResult.Success)
                            tag.NameTranslations.SetTranslation(lang.LangCode, translationResult.Text);
                        else
                            tag.NameTranslations.SetTranslation(lang.LangCode, result);
                    }
                    else
                        tag.NameTranslations.SetTranslation(lang.LangCode, result);
                }

                Project.CurrentProject.Tags.Add(tag);
                Project.CurrentProject.Save();

                RefreshTags();
            }
            else
                MessageBox.Show(this, "Bitte geben Sie einen gültigen Namen ein!", "Ungültiger Name", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ImageControlTag_ImageAdded(object sender, string path)
        {
            if (currentSelectedTag == null)
                return;


            string imageFilePath = System.IO.Path.Combine(Project.CurrentProject.RawTagFolder, currentSelectedTag.ID + ".png");
            if (System.IO.File.Exists(imageFilePath))
            {
                try
                {
                    System.IO.File.Delete(imageFilePath);
                }
                catch
                {

                }
            }

            try
            {
                if (!System.IO.Directory.Exists(Project.CurrentProject.RawTagFolder))
                    System.IO.Directory.CreateDirectory(Project.CurrentProject.RawTagFolder);

                System.IO.File.Copy(path, imageFilePath, true);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fehler beim Kopieren des Bildes: {ex.Message}", "TagImage");
            }
        }

        private void ButtonEditTag_Click(object sender, RoutedEventArgs e)
        {
            if (isInEditMode)
            {
                SetTagMode(false);
                isInEditMode = false;

                // Save
                // Image gets saved in ImageControlTag_ImageAdded
                // Translations are saved automatically (in TranslationItem)
                currentSelectedTag.Name = TextTagElementName.Text;            
                Project.CurrentProject.Save();
                RefreshTags();
            }
            else
            {
                // Prevent editing of nothing
                if (currentSelectedTag == null)
                    return;

                isInEditMode = true;
                SetTagMode(true);
            }            
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            if (ListTags.SelectedItem is Tag t && MessageBox.Show(this, $"Sind Sie sich wirklich sicher, dass Sie den Tag \"{t.Name}\" wirklich löschen möchten?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Delete this tag also in every blog (if set there)
                foreach (var assoc in Project.CurrentProject.BlogItems)
                {
                    foreach (var item in assoc.Items)
                    {
                        if (item.Tags.Contains(t.ID))
                            item.Tags.Remove(t.ID);
                    }
                }

                // Delete image file from tag
                string imageFilePath = System.IO.Path.Combine(Project.CurrentProject.RawTagFolder, currentSelectedTag.ID + ".png");
                try
                {
                    System.IO.File.Delete(imageFilePath);
                }
                catch
                { }

                Project.CurrentProject.Tags.Remove(t);
                Project.CurrentProject.Save();

                RefreshTags();
            }
        }

        private void ListTags_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ListTags.SelectedItem == null)
            {
                TabHolder.Visibility = Visibility.Hidden;
                return;
            }
            else
                TabHolder.Visibility = Visibility.Visible;

            var tag = ListTags.SelectedItem as Tag;
            currentSelectedTag = tag;

            TextTagElementName.Text = tag.Name;

            // Try to load tag image
            string imagePath = System.IO.Path.Combine(Project.CurrentProject.RawTagFolder, currentSelectedTag.ID + ".png");
            ImageControlTag.LoadImage(imagePath);

            TranslationTagName.Initalize(tag.NameTranslations, null);
            TranslationTagName.SetReadOnly();
            TranslationTagDescription.Initalize(tag.DescriptionTranslations, null);
            TranslationTagDescription.SetReadOnly();
        }

        #endregion

        #region Other Events
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            var height = MainGrid.RowDefinitions[MainGrid.RowDefinitions.Count - 1].ActualHeight;
            Settings.Instance.LogHeight = height;
            Settings.Instance.Save();


            if (blogPreviewWindow != null)
                blogPreviewWindow.Close();
        }

        private void CheckOwnUrlName_Checked(object sender, RoutedEventArgs e)
        {
            if (currentSelectedBlogItem == null)
                return;

            if (!CheckOwnUrlName.IsChecked.Value)
                TextUrlName.Text = TextBlogTitle.Text.CreateUrlTitle();
        }

        private void TreeViewLangItems_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Controls.TreeViewItem treeViewItem = GUIHelper.VisualUpwardSearch(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                if (treeViewItem.Header is PageViewModel p)
                    CurrentSelectedPage = p;
                else
                {
                    var scopeGUI = TreeViewLangItems.SelectedItem as ScopeViewModel;
                    currentSelectedScope = scopeGUI;

                    // Search until found a parent which is a TreeViewItem an has Page as it's Header!
                    UIElement item = GUIHelper.VisualUpwardSearch(e.OriginalSource as DependencyObject);

                    while (item != null)
                    {
                        item = (UIElement)VisualTreeHelper.GetParent(item as DependencyObject);

                        if (item != null && item is System.Windows.Controls.TreeViewItem tvi && tvi.Header is PageViewModel page)
                        {
                            CurrentSelectedPage = page;
                            break;
                        }
                    }
                }

                treeViewItem.Focus();
                e.Handled = false;

                return;
            }

            CurrentSelectedPage = null;
        }

        private void TreeViewLangItems_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TreeViewLangItems.SelectedItem is ItemViewModel item)
            {
                RefreshItemSource(item.ItemBehind);
                return;
            }

            if (TreeViewLangItems.SelectedItem is PageViewModel page)
                CurrentSelectedPage = page;

            if (TreeViewLangItems.SelectedItem is ScopeViewModel sp)
            {
                RefreshItemSource(CurrentSelectedPage, sp);
                return;
            }

            MenuButtonAdd.IsEnabled = false;
            TranslationItems.Children.Clear();
        }

        private void TabControlMain_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TabControlMain.SelectedIndex == 0)
                TabContent.IsSelected = true;
            else if (TabControlMain.SelectedIndex == 1)
                TabBlogs.IsSelected = true;
            else
                TabTags.IsSelected = true;
        }

        #endregion

        #region Other Methods

        private void AddNewPage()
        {
            string result = InputDialog.ShowInputDialog("Seiten Namen eingeben!", "Bitte geben Sie einen gültigen Seiten-Namen ein!");
            Project.CurrentProject.AddPage(result, Project.CurrentProject.Template);
            RefreshData();
        }

        private void ShowItemDialog()
        {
            if (CurrentSelectedPage == null || currentSelectedScope == null)
            {
                Logger.LogWarning("Es ist keine Seite oder kein Bereich ausgewählt!", "Translation");
                return;
            }

            ItemAddDialog itemAddDialog = new ItemAddDialog(CurrentSelectedPage.PageBehind, currentSelectedScope.ScopeBehind);
            bool? result = itemAddDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Project.CurrentProject.Items.Add(itemAddDialog.ResultItem);
                Project.CurrentProject.Save();
                RefreshData();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void ButtonSwitchDifferences_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        #endregion

        #region HTML Preview Section

        private bool isLoaded = false;

        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            isLoaded = true;
        }

        public async Task GenerateBlogWebbrowserPreview(BlogItem bg, string lang)
        {
            string htmlCodePreview = System.IO.File.ReadAllText(System.IO.Path.Combine(Project.CurrentProject.BlogTemplateFolder, Consts.IndexFile));
            string htmlCodePreviewDark = System.IO.File.ReadAllText(System.IO.Path.Combine(Project.CurrentProject.BlogTemplateFolder, Consts.IndexDarkFile));

            if (isLoaded)
                Browser.Source = new Uri("about:blank");
            if (blogPreviewWindow != null)
                blogPreviewWindow.Clear();

            await SaveHtmlPreview(CreateHtmlPreview(htmlCodePreview, lang, bg), System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, Consts.IndexFile));
            await SaveHtmlPreview(CreateHtmlPreview(htmlCodePreviewDark, lang, bg), System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, Consts.IndexDarkFile));

            if (isLoaded)
            {
                string fileName = ThemeManager.Current.DetectTheme().BaseColorScheme == "Dark" ? Consts.IndexDarkFile : Consts.IndexFile;
                Browser.Source = new Uri(System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, fileName));
            }

            if (blogPreviewWindow != null)
                blogPreviewWindow.Refresh();
        }

        private async Task SaveHtmlPreview(string content, string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch
            { }

            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fehler beim Zugriff auf die Datei {path}. Fehler: {ex.Message}", "HTMLPreview");
            }
        }

        private string CreateHtmlPreview(string htmlCode, string lang, BlogItem bg)
        {
            // Replace all guids with valid links
            string content = TextBlogContent.Text;
            var assoc = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang).FirstOrDefault();

            if (assoc != null)
            {
                foreach (var blog in assoc.Items)
                {
                    string langRepl = (lang != Project.CurrentProject.MainLanguage ? $"{lang}/" : string.Empty);

                    if (!blog.IsTranslated)
                        content = content.Replace(blog.ID.ToString(), $"{Project.CurrentProject.ProjectUrl}/{langRepl}blog/{blog.UrlName.CreateUrlTitle()}");
                    else
                        content = content.Replace(blog.ParentID.ToString(), $"{Project.CurrentProject.ProjectUrl}/{langRepl}blog/{blog.UrlName.CreateUrlTitle()}");
                }
            }

            htmlCode = htmlCode.Replace("{0}", lang);
            htmlCode = htmlCode.Replace("{1}", bg.Title);
            htmlCode = htmlCode.Replace("{2}", bg.ImageFileName);
            htmlCode = htmlCode.Replace("{3}", bg.PublishedDate.ToString("dd.MM.yyyy"));
            htmlCode = htmlCode.Replace("{4}", content);
            htmlCode = htmlCode.Replace("{5}", DateTime.Now.Year.ToString());

            return htmlCode;
        }

        private async void ButtonRefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            await RefreshHTMLPreview();
        }

        private async void TabControlEdit_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TabControlEdit.SelectedIndex == 2)
                await RefreshHTMLPreview();
        }

        private void ButtonNewPreviewWindow_Click(object sender, RoutedEventArgs e)
        {
            if (blogPreviewWindow != null)
                blogPreviewWindow.Activate();
            else
            {
                blogPreviewWindow = new BlogPreview();
                blogPreviewWindow.OnClosingPreviewWindow += BlogPreviewWindow_OnClosingPreviewWindow;
                blogPreviewWindow.Show();
            }
        }

        private void BlogPreviewWindow_OnClosingPreviewWindow(object sender, EventArgs e)
        {
            blogPreviewWindow = null;
        }

        private async Task RefreshHTMLPreview()
        {
            if (currentSelectedBlogItem == null)
                return;

            var lang = CurrentSelectedBlogLanguage;
            await GenerateBlogWebbrowserPreview(currentSelectedBlogItem, lang.LangCode ?? Project.CurrentProject.MainLanguage);
        }

        #endregion

        #region Log

        private readonly FlowDocument flowDocument = new FlowDocument { FontFamily = new FontFamily("Consolas") };
        private readonly Paragraph currentParagraph = new Paragraph();
        private readonly List<string> log = new List<string>();
        private bool isInitalized = false;

        private void LogHolder_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            LogScrollViewer.ScrollToVerticalOffset(LogScrollViewer.VerticalOffset - e.Delta);
        }

        private void ButtonClearLog_Click(object sender, RoutedEventArgs e)
        {
            currentParagraph.Inlines.Clear();
            log.Clear();
        }

        private void InitalizeLog()
        {
            flowDocument.Blocks.Add(currentParagraph);
            LogHolder.Document = flowDocument;

            Logger.OnAddedLogEntry += Logger_OnAddedLogEntry;
        }

        private void Logger_OnAddedLogEntry(LogEntry logEntry)
        {
            // Check log level first
            if (Settings.Instance.LogLevel == LogLevelSetting.None)
                return;

            if (Settings.Instance.LogLevel != LogLevelSetting.All && logEntry.Level == LogLevel.Debug)
                return;

            if (Settings.Instance.LogLevel == LogLevelSetting.OnlyWarningsAndErros && (logEntry.Level == LogLevel.Debug || logEntry.Level == LogLevel.Information))
                return;

            if (!isInitalized)
                return;

            Dispatcher.Invoke(() =>
            {
                log.Add(logEntry.ToString());
                AddLogEntry(logEntry);
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        private void AddLogEntry(LogEntry entry)
        {
            if (TabControlLog.Visibility == Visibility.Collapsed)
                TabControlLog.Visibility = Visibility.Visible;

            // Get image
            BitmapImage bi = new BitmapImage { CacheOption = BitmapCacheOption.OnLoad };
            bi.BeginInit();
            string resourceName = string.Empty;
            SolidColorBrush foregroundBrush = null;


            switch (entry.Level)
            {
                case LogLevel.Debug:
                case LogLevel.Information:
                    {
                        resourceName = "info.png";
                        foregroundBrush = FindResource("BlackBrush") as SolidColorBrush;
                    }
                    break;
                case LogLevel.Warning:
                    {
                        resourceName = "warning.png";
                        foregroundBrush = new SolidColorBrush(Colors.Orange);
                    }
                    break;
                case LogLevel.Error:
                    {
                        resourceName = "error.png";
                        foregroundBrush = new SolidColorBrush(Colors.OrangeRed);
                    }
                    break;
            }

            bi.UriSource = new Uri($"pack://application:,,,/Translator;Component/resources/icons/log/{resourceName}");
            bi.EndInit();

            currentParagraph.Inlines.Add(new InlineUIContainer(new System.Windows.Controls.Image() { Source = bi, Width = 20, Margin = new Thickness(0, 2, 2, 0) }) { BaselineAlignment = BaselineAlignment.Bottom });
            currentParagraph.Inlines.Add(new Run($"[{entry.Timestamp.ToShortDateString()} "));
            currentParagraph.Inlines.Add(new Run(entry.Timestamp.ToShortTimeString()));
            currentParagraph.Inlines.Add(new Run($" ({entry.Module})]: "));
            currentParagraph.Inlines.Add(new Run { Text = entry.Message, Foreground = foregroundBrush, BaselineAlignment = BaselineAlignment.Bottom });
            currentParagraph.Inlines.Add(new LineBreak());
            LogScrollViewer.ScrollToEnd();
        }

        private async void ButtonExportLogToTelegram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string content = string.Join(Environment.NewLine, log.ToArray()); // currentParagraph.Inlines.Select(line => line.ContentStart.GetTextInRun(LogicalDirection.Forward)));

                    List<KeyValuePair<string, string>> postContent = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("content", content),
                        new KeyValuePair<string, string>("version", typeof(MainWindow).Assembly.GetName().Version.ToString(3)),
                        new KeyValuePair<string, string>("machine", Environment.MachineName),
                    };

                    await client.PostAsync(Project.CurrentProject.TelegramProtocolSendUrl, new FormUrlEncodedContent(postContent));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fehler beim Exportieren des Logs: {ex.Message}", "TelegramExporter");
            }
        }
        #endregion

        #region Project

        private readonly DispatcherTimer closingMenuTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(500) };

        private void ListRecentlyOpenedDocuments_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ListRecentlyOpenedDocuments.SelectedItem is ShortProject p)
            {
                // Check if file exists
                if (!System.IO.File.Exists(p.Path))
                {
                    if (MessageBox.Show(this, "Das ausgewählte Projekt existiert nicht mehr! Möchten Sie es löschen?", "Nicht mehr vorhanden", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        RecentlyOpenedProjects.Remove(p.Path);
                }
                else
                {
                    closingMenuTimer.Start();
                    Project.LoadProject(p.Path);
                }

                ListRecentlyOpenedDocuments.SelectedItem = null;
            }
        }

        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            closingMenuTimer.Stop();
            AppMenu.IsDropDownOpen = false;
        }

        private void MenuButtonLoadProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog() { Filter = "Translator Projektdatei (*.tproj)|*.tproj" };
            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
                Project.LoadProject(ofd.FileName);
        }

        private void MenuButtonCreateNewProject_Click(object sender, RoutedEventArgs e)
        {
            new CreateNewProjectDialog().ShowDialog();
        }

        public void SetTitle()
        {
            if (Project.CurrentProject != null)
                Title = $"Translator - Projekt: {Project.CurrentProject.Name}";
            else
                Title = $"Translator - Kein Projekt geöffnet";
        }

        private void RecentlyOpenedProjects_RecentlyOpenedProjectsChanged()
        {
            ListRecentlyOpenedDocuments.ItemsSource = null;
            ListRecentlyOpenedDocuments.Items.Clear();
            ListRecentlyOpenedDocuments.ItemsSource = RecentlyOpenedProjects.Projects;
        }

        private void Project_OnProjectChanged()
        {
            // Enable or disable buttons due to the fact if a project is loaded
            SetProjectFeatureState(Project.CurrentProject != null);

            if (Project.CurrentProject == null)
                return;

            RecentlyOpenedProjects.AddProject(Project.CurrentProject);

            string oldLanguage = CurrentSelectedBlogLanguage?.LangCode;
            CurrentSelectedBlogLanguage = Project.CurrentProject.Languages.FirstOrDefault(p => p.LangCode == oldLanguage); // may be null though
            ButtonExportLogToTelegram.Visibility = string.IsNullOrEmpty(Project.CurrentProject.TelegramProtocolSendUrl) ? Visibility.Collapsed : Visibility.Visible;
            RefreshData();
            RefreshBlogs();
            RefreshTags();
            SetTitle();

            TabGeneral.IsSelected = true;
        }

        private void SetProjectFeatureState(bool isProjectLoaded)
        {
            TabContent.IsEnabled =
            TabGeneral.IsEnabled =
            TabControlMain.IsEnabled = 
            TabBlogs.IsEnabled = 
            MenuButtonOpenWorkspace.IsEnabled =
            MenuButtonOpenPublicFolder.IsEnabled =
            MenuGenerateJson.IsEnabled =
            MenuUploadFTP.IsEnabled =
            MenuLanguage.IsEnabled =
            MenuSettings.IsEnabled = isProjectLoaded;
        }

        #endregion

        #region Report

        private int CountWords(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            return input.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private int CountHtmlWords(string html)
        {
            // HTML
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNode scriptNode = null;
            int counter = 0;
            foreach (var node in document.DocumentNode.DescendantsAndSelf())
            {
                if (node.Name == "script")
                {
                    scriptNode = node;
                    continue;
                }

                if (scriptNode != null && scriptNode.ChildNodes.Contains(node))
                    continue;

                if (node.ChildNodes.Count == 0 && node.InnerHtml.Trim().Length > 0)
                    counter += CountWords(node.InnerHtml.Trim());
            }

            return counter;
        }
    

        private int CountWords()
        {
            if (Project.CurrentProject == null)
                return 0;

            int result = 0;
            foreach (var item in Project.CurrentProject.Items)
            {
                string content = item.Translate("en");

                // Ignore changelog
                if (item.IsMultiLineText &&
                    item.Key == "data" &&
                    item.DPages.Contains(Project.CurrentProject.Pages.FirstOrDefault(p => p.Name == "changelog")))
                    continue;

                if (item.IsMultiLineText)
                    result += CountHtmlWords(content);
                else 
                    result += CountWords(content);
            }

            return result;
        }

        private void ButtonGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            int result = CountWords();

            MessageBox.Show("Wörter (ohne Blog Artikel): " + result, "Ergebnis", MessageBoxButton.OK, MessageBoxImage.Information);

            // Check blogs
            var assoc = Project.CurrentProject.BlogItems.FirstOrDefault(p => p.LangCode == Project.CurrentProject.MainLanguage);

            List<string> missingBlogs = new List<string>();
            int count = 0;

            if (assoc != null)
            {
                foreach (var item in assoc.Items)
                {
                    // Check if this blog is in all languages (except than Main) 
                    int tempCount = 0;
                    foreach (var lang in Project.CurrentProject.Languages)
                    {
                        if (lang.LangCode == Project.CurrentProject.MainLanguage)
                            continue;

                        var temp = Project.CurrentProject.BlogItems.FirstOrDefault(p => p.LangCode == lang.LangCode);

                        if (!temp.Items.Any(i => i.ParentID == item.ID))
                        {
                            missingBlogs.Add($"{item.Title} ({lang.Name})");
                            count++;
                            tempCount++;
                        }
                    }

                    if (tempCount > 0)
                        missingBlogs.Add(string.Empty);
                }
            }

            if (count > 0)
                MessageBox.Show(string.Join(Environment.NewLine, missingBlogs), $"Fehlende Blogs: {count}");
            else
                MessageBox.Show("Keine fehlenden Blogs gefunden!", "Fehlende Blogs!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    #region Converter
    public class ScopeNameToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScopeViewModel spv)
            {
                var sp = spv.ScopeBehind;
                string icon = "scope.png";

                if (sp.GetParentPages().Count >= 2)
                    icon = "special.png";
                else if (sp == Scope.GENERAL)
                    icon = "scope.png";
                else if (Project.CurrentProject.GeneralScopes.Contains(sp))
                    icon = "overall.png";

                BitmapImage bm = new BitmapImage();
                bm.BeginInit();
                bm.UriSource = new Uri($"pack://application:,,,/Translator;component/resources/icons/{icon}");
                bm.EndInit();

                return bm;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ScopeToToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScopeViewModel spv)
            {
                var parents = spv.ScopeBehind.GetParentPages();
                int count = parents.Count;
                if (count > 1)
                {
                    string parentPages = "Parents: ";
                    for (int i = 0; i < count; i++)
                    {
                        parentPages += parents[i].DisplayName;

                        if (i != count - 1)
                            parentPages += ", ";
                    }

                    return parentPages;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VisibilityToLogToggleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility && visibility == Visibility.Visible)
                return true;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }
    }

    public class WidthWithMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && int.TryParse(parameter.ToString(), out int m))
                return d - m;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBoldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return FontWeights.Bold;

            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}