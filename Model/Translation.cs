using Translator.Helper;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace Translator.Model
{
    public class Translation : INotifyPropertyChanged
    {
        private string text;
        private string language;
        private bool isApproved;

        private static readonly XmlDocument dummyDoc = new XmlDocument();

        [XmlElement("htmlValue")]
        public XmlCDataSection HtmlValueCDATA
        {
            get { return dummyDoc.CreateCDataSection(Text.CleanUp()); }
            set { Text = value?.Data.CleanUp(); }
        }

        [XmlIgnore]
        public string Text
        {
            get => text.CleanUp();
            set
            {
                if (value != text)
                {
                    text = value.CleanUp();
                    FirePropertyChanged("Text");
                }
            }
        }

        public string Language
        {
            get => language;
            set
            {
                if (value != language)
                {
                    language = value;
                    FirePropertyChanged("Language");
                }
            }
        }

        public bool IsApproved
        {
            get => isApproved;
            set
            {
                if (value != isApproved)
                {
                    isApproved = value;
                    FirePropertyChanged("IsApproved");
                }
            }
        }

        public Translation() { }

        public Translation(string language, string text)
        {
            Language = language;
            Text = text;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void FirePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"[{Language}]:{Text} ({(IsApproved ? "true" : "false")})";
        }
    }
}
