using System.Collections.Generic;

namespace Translator.Model.Templates
{
    public class TemplateItem
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public bool IsHtml { get; set; }

        public List<string> ParentPages { get; set; } = new List<string>();

        public List<string> ParentScopes { get; set; } = new List<string>();

        public ItemType ItemType { get; set; } = ItemType.Scopes;     

        public override string ToString()
        {
            return Name;
        }
    }
}
