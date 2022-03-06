using System.Collections.ObjectModel;

namespace Translator.Model.Blog
{
    public class BlogItemLanguageAssoc
    {
        public ObservableCollection<BlogItem> Items { get; set; } = new ObservableCollection<BlogItem>();

        public string LangCode { get; set; }

        public override string ToString()
        {
            return LangCode;
        }
    }
}
