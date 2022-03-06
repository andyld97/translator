using System.Windows;
using System.Windows.Media;

namespace Translator.Helper
{
    public static class GUIHelper
    {
        public static System.Windows.Controls.TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is System.Windows.Controls.TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as System.Windows.Controls.TreeViewItem;
        }

        /// <summary>
        /// Get bounds from a child of a visual
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Rect BoundsRelativeTo(this FrameworkElement child, Visual parent)
        {
            try
            {
                GeneralTransform gt = child.TransformToAncestor(parent);
                return gt.TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
            }
            catch
            { }

            return new Rect();
        }
    }
}
