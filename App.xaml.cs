using ControlzEx.Theming;
using Translator.Model;
using System;
using System.Linq;
using System.Windows;

namespace Translator
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            var args = Environment.GetCommandLineArgs();

            if (args.Length >= 2)
            {
                // First argument is the file path of the executable
                string projectToLoad = args[1];
                Project.LoadProject(projectToLoad);
            }
            // Remember: This project which is opened most recently is on the top of the list (even when it was on the list before)!
            else if (Settings.Instance.LoadLastProjectOnStartup && RecentlyOpenedProjects.Projects.FirstOrDefault() != null)
                Project.LoadProject(RecentlyOpenedProjects.Projects.FirstOrDefault().Path);

            try
            {
                if (Project.CurrentProject != null && !System.IO.Directory.Exists(Project.CurrentProject.BlogImagesFolder))
                    System.IO.Directory.CreateDirectory(Project.CurrentProject.BlogImagesFolder);
            }
            catch
            { }            

            base.OnStartup(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unerwarteter Fehler aufgetreten: " + e.ExceptionObject?.ToString());
        }
    }
}
