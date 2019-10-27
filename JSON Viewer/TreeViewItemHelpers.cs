using System.Windows;
using System.Windows.Controls;

namespace JSON_Viewer
{
    //https://stackoverflow.com/a/52495224
    public static class TreeViewItemHelper
    {
        public static bool GetBringSelectedItemIntoView(TreeViewItem treeViewItem)
        {
            return (bool)treeViewItem.GetValue(BringSelectedItemIntoViewProperty);
        }

        public static void SetBringSelectedItemIntoView(TreeViewItem treeViewItem, bool value)
        {
            treeViewItem.SetValue(BringSelectedItemIntoViewProperty, value);
        }

        public static readonly DependencyProperty BringSelectedItemIntoViewProperty =
            DependencyProperty.RegisterAttached(
                "BringSelectedItemIntoView",
                typeof(bool),
                typeof(TreeViewItemHelper),
                new UIPropertyMetadata(false, BringSelectedItemIntoViewChanged));

        private static void BringSelectedItemIntoViewChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (!(args.NewValue is bool))
                return;

            if (!(obj is TreeViewItem item))
                return;

            if ((bool)args.NewValue)
                item.Selected += OnTreeViewItemSelected;
            else
                item.Selected -= OnTreeViewItemSelected;
        }

        private static void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as TreeViewItem;
            item?.BringIntoView();

            // prevent this event bubbling up to any parent nodes
            e.Handled = true;
        }
    }
}
