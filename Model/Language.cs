using Translator.Helper;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace Translator.Model
{
    public class Language : INotifyPropertyChanged
    {
        private bool isSelected = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }

        public string LangCode { get; set; }

        [XmlIgnore]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                }
            }
        }

        public Language() { }

        public Language(string name, string langCode)
        {
            this.Name = name;
            this.LangCode = langCode;
        }

        public BitmapImage Flag => ImageHelper.LoadFlag(LangCode);

        public override string ToString()
        {
            return LangCode;
        }
    }
}
