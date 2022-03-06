using System;

namespace Translator.Model.ViewModel
{
    public class ItemViewModel
    {
        public Guid ID { get; set; }

        public Item ItemBehind { get; set; }

        public string Key { get; set; }
    }
}
