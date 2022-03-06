using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Translator.Model.ViewModel
{
    public class PageViewModel : TreeViewItem
    {
        public Guid ID { get; set; }

        public Page PageBehind { get; set; }

        public string DisplayName { get; set; }

        public ObservableCollection<ScopeViewModel> ScopesAll { get; set; } = new ObservableCollection<ScopeViewModel>();

        public static ObservableCollection<PageViewModel> BuildViewModel(ObservableCollection<PageViewModel> oldData = null, bool selectItemsWithMissingTranslations = false)
        {
            ObservableCollection<PageViewModel> pageGUIs = new ObservableCollection<PageViewModel>();

            if (Project.CurrentProject == null)
                return pageGUIs;

            foreach (var page in Project.CurrentProject.PagesAll)
            {
                var pg = new PageViewModel()
                {
                    ID = page.ID,
                    PageBehind = page,
                    DisplayName = page.DisplayName
                };

                if (oldData != null)
                {
                    // Check if old page was expanded or selected and apply values
                    var oldPage = oldData.FirstOrDefault(p => p.ID == pg.ID);
                    if (oldPage != null)
                    {
                        pg.IsExpanded = oldPage.IsExpanded;
                        pg.IsSelected = oldPage.IsSelected;
                    }
                }

                foreach (var scope in page.ScopesAll)
                {
                    ScopeViewModel sg = new ScopeViewModel()
                    {
                        ID = scope.ID,
                        ScopeBehind = scope,
                        DisplayName = scope.DisplayName
                    };

                    if (oldData != null)
                    {
                        // Check if old scope was expanded or selected and apply values
                        var oldPage = oldData.FirstOrDefault(p => p.ID == pg.ID);
                        if (oldPage != null)
                        {
                            var oldScope = oldPage.ScopesAll.FirstOrDefault(p => p.ID == sg.ID);
                            if (oldScope != null)
                            {
                                sg.IsExpanded = oldScope.IsExpanded;
                                sg.IsSelected = oldScope.IsSelected;
                            }
                        }
                    }


                    bool anyScopesWithMissingTranslations = false;
                    foreach (var item in page.GetItems(scope))
                    {
                        if (!item.IsTranslatedCompletly && selectItemsWithMissingTranslations)
                            anyScopesWithMissingTranslations = true;

                        if (!selectItemsWithMissingTranslations || (selectItemsWithMissingTranslations && !item.IsTranslatedCompletly))
                            sg.Items.Add(new ItemViewModel() { ID = item.ID, Key = item.Key, ItemBehind = item });
                    }

                    if (!selectItemsWithMissingTranslations || (anyScopesWithMissingTranslations && selectItemsWithMissingTranslations))
                        pg.ScopesAll.Add(sg);
                }

                if (!selectItemsWithMissingTranslations || (pg.ScopesAll.Count > 0 && selectItemsWithMissingTranslations))
                    pageGUIs.Add(pg);
            }

            return pageGUIs;
        }
    }
}
