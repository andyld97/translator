using System;
using System.Collections.Generic;
using System.Linq;

namespace Translator.Model
{
    public class Scope
    {
        public static readonly Scope GENERAL = new Scope("*") { ID = Guid.Empty };

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

        public Scope() { }

        public Scope(string name)
        {
            this.Name = name;
        }

        public static bool operator ==(Scope s1, Scope s2)
        {
            if (s1 is null)
                return s2 is null;

            return s1.Equals(s2);
        }

        public static bool operator !=(Scope s1, Scope s2)
        {
            return !(s1 == s2);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Scope s)
                return s.ID == this.ID;

            return false;
        }

        public List<Page> GetParentPages()
        {
            List<Page> parentPages = new List<Page>();

            if (this == Scope.GENERAL)
            {
                foreach (var scope in Project.CurrentProject.GeneralScopes)
                {
                    if (scope == this)
                    {
                        foreach (var page in Project.CurrentProject.PagesAll)
                            parentPages.Add(page);
                        break;
                    }
                }
            }
            else
            {
                foreach (var page in Project.CurrentProject.PagesAll)
                {
                    if (page.Scopes.Contains(this))
                        parentPages.Add(page);
                }
            }


            return parentPages;
        }

        public void ClearItems(Page parent)
        {
            List<Item> itemsToRemove = new List<Item>();

            if (parent == Page.GENERAL)
                itemsToRemove = Project.CurrentProject.Items.Where(p => p.Scope == this).ToList();
            else
                itemsToRemove = Project.CurrentProject.Items.Where(p => p.DPages.Contains(parent) && p.Scope == this).ToList();

            foreach (var item in itemsToRemove)
                Project.CurrentProject.Items.Remove(item);
        }

        public override string ToString()
        {
            return $"{ID}:{DisplayName}";
        }
    }
}