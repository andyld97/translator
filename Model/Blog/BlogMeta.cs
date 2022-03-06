using Translator.Helper;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace Translator.Model.Blog
{
    public class BlogMeta : ICloneable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string description;
        private string[] keywords;

        [JsonProperty("description")]
        public string Description
        {
            get => description.CleanUp();
            set
            {
                if (value != description)
                {
                    description = value.CleanUp();
                    FirePropertyChanged("Description");
                }
            }
        }

        [JsonProperty("keywords")]
        public string[] Keywords
        {
            get => keywords.CleanUp();
            set
            {
                if (value != keywords)
                {
                    keywords = value.CleanUp();
                    FirePropertyChanged("Keywords");
                }
            }
        }

        public void FirePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Clone()
        {
            return new BlogMeta() { Description = this.Description, Keywords = this.Keywords };
        }
    }
}
