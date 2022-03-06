using System.Collections.Generic;

namespace Translator.Model.Templates
{
    public class Template
    {
        public string Name { get; set; }

        public List<string> Pages { get; set; }

        public List<Language> Languages { get; set; } = new List<Language>();

        public List<TemplateScope> TemplateScopes { get; set; } = new List<TemplateScope>();

        public List<TemplateItem> TemplateItems { get; set; } = new List<TemplateItem>();

        public string DeepLKey { get; set; }

        public string GoogleCloudProjectName { get; set; }

        public string TelegramUrl { get; set; }

        public string GoogleCloudProjectFileContent { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
