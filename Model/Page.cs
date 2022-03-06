using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace Translator.Model
{
    public class Page
    {
        public static readonly Page GENERAL = new Page("*") { ID = Guid.Empty };

        public Guid ID { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public string DisplayName
        {
            get
            {
                if (Name == "*")
                    return "general";
                else
                    return Name;
            }
        }

        public ObservableCollection<Scope> Scopes { get; set; } = new ObservableCollection<Scope>();

        [XmlIgnore]
        public ObservableCollection<Scope> ScopesAll => GetAllScopes(Project.CurrentProject);

        public ObservableCollection<Scope> GetAllScopes(Project project)
        {
            ObservableCollection<Scope> scopes = new ObservableCollection<Scope>();
            foreach (var scope in Scopes)
                scopes.Add(scope);

            foreach (var scope in project.GeneralScopes)
                scopes.Add(scope);

            scopes.Add(Scope.GENERAL);
            return scopes;
        }

        public Page() { }

        public Page(string name)
        {
            this.Name = name;
        }

        public static bool operator ==(Page s1, Page s2)
        {
            if (s1 is null)
                return s2 is null;

            return s1.Equals(s2);
        }

        public static bool operator !=(Page s1, Page s2)
        {
            return !(s1 == s2);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Page s)
                return s.ID == this.ID;

            return false;
        }

        public List<Item> GetItems(Scope sp = null)
        {
            List<Item> items = new List<Item>();
            if (sp == null)
            {
                foreach (var scope in ScopesAll)
                {
                    var langItems = Project.CurrentProject.Items.Where(p => p.Scope == scope && (p.Pages.Contains(ID) || p.Pages.Count == 0 || Project.CurrentProject.GeneralScopes.Contains(scope) || (p.Scope == Scope.GENERAL && p.Pages.Contains(Scope.GENERAL.ID)))).ToList();
                    items.AddRange(langItems);
                }
            }
            else
            {
                var langItems = Project.CurrentProject.Items.Where(p => p.Scope == sp && (p.Pages.Contains(ID) || p.Pages.Count == 0 || Project.CurrentProject.GeneralScopes.Contains(sp) || (p.Scope == Scope.GENERAL && p.Pages.Contains(Scope.GENERAL.ID)))).ToList();
                items.AddRange(langItems);
            }

            return items;
        }

        public void ClearItems()
        {
            var itemsToRemove = Project.CurrentProject.Items.Where(p => p.DPages.Contains(this)).ToList();

            foreach (var item in itemsToRemove)
                Project.CurrentProject.Items.Remove(item);
        }

        public void ClearScopesIfRequired()
        {
            if (Scopes.Count == 1)
            {
                var sp = Scopes.First();
                // Only remove clear the scope if it's only on this page!
                if (Project.CurrentProject.Pages.Any(x => x != this && x.Scopes.Contains(sp)))
                    sp.ClearItems(this);
                Scopes.Clear();
            }
        }

        public override string ToString()
        {
            return $"{ID}: {DisplayName}";
        }
    }
}
