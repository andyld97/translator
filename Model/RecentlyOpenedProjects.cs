using Helper;
using System.Collections.Generic;
using System.Linq;

namespace Translator.Model
{
    public class RecentlyOpenedProjects
    {
        private static readonly string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "RecentProjects.xml");

        public static List<ShortProject> Projects = Load();

        public delegate void recentlyOpenedProjectsChanged();
        public static event recentlyOpenedProjectsChanged RecentlyOpenedProjectsChanged;

        private static List<ShortProject> Load()
        {
            List<ShortProject> result = null;
            try
            {
                result = Serialization.Read<List<ShortProject>>(path, Serialization.Mode.XML);
            }
            catch
            {
                // Silence is golden
            }

            if (result == null)
            {
                result = new List<ShortProject>();
                Save(result);
            }

            return result;
        }

        public static void Save()
        {
            Save(Projects);
        }

        private static void Save(List<ShortProject> projects)
        {
            try
            {
                Serialization.Save(path, projects, Serialization.Mode.XML);
            }
            catch
            {
                // Silence is golden
            }
        }

        public static void AddProject(Project project)
        {
            if (project == null)
                return;

            // Check if documents existis
            var result = Projects.Where(d => d.Path == project.ProjectFilePath).FirstOrDefault();

            if (result == null)
                Projects.Insert(0, new ShortProject() { Path = project.ProjectFilePath, Name = project.Name, Description = project.Description });
            else
            {
                // Place it to top
                Projects.Remove(result);
                Projects.Insert(0, result);
            }

            // Limit to value of recently used docuemnts
            if (Projects.Count > Consts.LastRecentlyOpenedDocuments)
                Projects.RemoveAt(Projects.Count - 1);

            Save(Projects);
            RecentlyOpenedProjectsChanged?.Invoke();
        }

        public static void Remove(string fileName)
        {
            Projects.Remove(Projects.Where(f => f.Path == fileName).FirstOrDefault());
            Save(Projects);

            RecentlyOpenedProjectsChanged?.Invoke();
        }
    }
}
