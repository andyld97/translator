using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace Translator.Model
{
    public class Item : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region Private Fields
        private string key;
        private Guid scopeID;
        private bool isMultiLineText;
        private long maxLength = 0;
        #endregion

        #region Public Properties

        /// <summary>
        /// A unique identifier of this item
        /// </summary>
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The unique identifier of an lang item
        /// </summary>
        public string Key
        {
            get => key;
            set
            {
                if (value != key)
                {
                    key = value;
                    FirePropertyChanged("Key");
                }
            }
        }

        /// <summary>
        /// A language item can be used in multiple pages
        /// </summary>
        public List<Guid> Pages { get; set; } = new List<Guid>();

        [XmlIgnore]
        public List<Page> DPages
        {
            get
            {
                List<Page> pages = new List<Page>();
                foreach (Guid guid in Pages)
                {
                    var page = Project.CurrentProject.Pages.Where(p => p.ID == guid).FirstOrDefault();
                    if (page != null)
                        pages.Add(page);
                }

                return pages;
            }
        }

        public string Path
        {
            get
            {
                string path = "/";

                if (DPages.Count == 0 || DPages.Contains(Page.GENERAL))
                    path += "general";
                else
                    path += "{" + string.Join(",", DPages.Select(d => d.DisplayName)) + "}";

                path += $"/{Scope?.DisplayName}/{key}";
                return path;
            }
        }

        public string ToJsonPath(Page page)
        {
            if (page == null)
                return Key;

            string path = string.Empty;

            if (DPages.Contains(page) && Pages.Count <= 1)
                path += "page";
            else
                path += "general";

            if (Scope != Scope.GENERAL)
                path += $".{Scope?.DisplayName}.{key}";
            else
                path += $".{key}";
            
            return path;
        }

        /// <summary>
        /// The category of the language item which is like a scope
        /// </summary>
        public Guid ScopeID
        {
            get => scopeID;
            set
            {
                if (value != scopeID)
                {
                    scopeID = value;
                    FirePropertyChanged("ScopeID");
                }
            }
        }

        [XmlIgnore]
        public Scope Scope
        {
            get
            {
                Scope result = Project.CurrentProject.GeneralScopes.Where(p => p.ID == ScopeID).FirstOrDefault();
                if (result != null)
                    return result;

                if (result == null)
                {
                    foreach (var dat in Project.CurrentProject.PagesAll)
                    {
                        foreach (var x in dat.ScopesAll)
                        {
                            if (x.ID == ScopeID)
                                return x;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Determines if this a multi-line text
        /// </summary>
        public bool IsMultiLineText
        {
            get => isMultiLineText;
            set
            {
                if (value != isMultiLineText)
                {
                    isMultiLineText = value;
                    FirePropertyChanged("IsMultiLineText");
                }
            }
        }

        /// <summary>
        /// Determines the max length - if set to zero, max length is the max lenght of a string, but not infinite!
        /// </summary>
        public long MaxTextLength
        {
            get => maxLength;
            set
            {
                if (value != maxLength)
                {
                    maxLength = value;
                    FirePropertyChanged("MaxTextLength");
                }
            }

        }

        /// <summary>
        /// Contains all translations for this items
        /// </summary>
        public ObservableCollection<Translation> Translations { get; set; } = new ObservableCollection<Translation>();

        #endregion

        public string Translate(string lang)
        {
            var item = Translations.Where(p => p.Language == lang).FirstOrDefault();
            if (item != null)
                return item.Text;

            return string.Empty;
        }

        public string Translate(Language lang, bool takeEnglishVersionIfEmpty)
        {
            string value = Translate(lang.LangCode);

            if (string.IsNullOrEmpty(value) && takeEnglishVersionIfEmpty)
                value = Translate("en");

            return value;
        }

        public void MergeTranslations(Item another)
        {
            if (another == null)
                return;

            foreach (var item in another.Translations)
            {
                if (Translations.Any(p => p.Language == item.Language))
                {
                    // trl cannot be null
                    var trl = Translations.Where(p => p.Language == item.Language).FirstOrDefault();

                    if (string.IsNullOrEmpty(trl.Text) || trl.Text != item.Text)
                        trl.Text = item.Text;

                    trl.IsApproved = item.IsApproved;
                }
                else
                    Translations.Add(new Translation(item.Language, item.Text) { IsApproved = item.IsApproved });
            }
        }

        public void SetTranslation(string lang, string text)
        {
            Translation t = Translations.Where(p => p.Language == lang).FirstOrDefault();
            if (t != null)
                t.Text = text;
            else
                Translations.Add(new Translation(lang, text));

            Project.CurrentProject.Save();
        }

        public void SetApproved(string lang, bool state)
        {
            Translation t = Translations.Where(p => p.Language == lang).FirstOrDefault();
            if (t != null)
                t.IsApproved = state;
            else
                Translations.Add(new Translation(lang, string.Empty) { IsApproved = state });

            Project.CurrentProject.Save();
        }

        [XmlIgnore]
        public bool IsTranslatedCompletly
        {
            get
            {
                if (Translations.Count == 0)
                    return false;

                return Translations.All(p => p.IsApproved || !string.IsNullOrEmpty(p.Text));
            }
        }

        private void FirePropertyChanged(string propertyName)
        {
            Project.CurrentProject?.Save();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{ID}:{Key}";
        }
    }
}