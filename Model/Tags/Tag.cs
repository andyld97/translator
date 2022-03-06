using System;
using System.Collections.ObjectModel;

namespace Translator.Model.Tags
{
    public class Tag
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }

        public Item NameTranslations { get; set; } = null;

        public Item DescriptionTranslations { get; set; } = null;

        public override string ToString()
        {
            return Name;
        }
    }
}