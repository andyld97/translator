using System;
using System.Collections.ObjectModel;

namespace Translator.Model.ViewModel
{
    public class ScopeViewModel : TreeViewItem
    {
        public Guid ID { get; set; }

        public Scope ScopeBehind { get; set; }

        public string DisplayName { get; set; }

        public ObservableCollection<ItemViewModel> Items { get; set; } = new ObservableCollection<ItemViewModel>();
    }
}
