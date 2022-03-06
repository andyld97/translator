using System.Collections.Generic;

namespace Translator.Model.Templates
{
    public class TemplateScope
    {
        public string Name { get; set; }

        public List<string> ParentPages { get; set; } = new List<string>();

        /// <summary>
        /// If set to true a unique scope will be created for each page
        /// </summary>
        public bool IsUniqueOnEveryPage { get; set; }

        public ScopeType ScopeType { get; set; }

        public List<TemplateItem> UniqueItems { get; set; } = new List<TemplateItem>();  

        public override string ToString()
        {
            return Name;
        }
    }

}
