using System.ComponentModel;
using System.Xml.Serialization;

namespace Translator.Model.ViewModel
{
    public class TreeViewItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isSelected = false;
        private bool isExpanded = false;

        [XmlIgnore]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    FirePropertyChanged("IsSelected");
                }
            }
        }

        [XmlIgnore]
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (value != isExpanded)
                {
                    isExpanded = value;
                    FirePropertyChanged("IsExpanded");
                }
            }
        }

        public void FirePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
