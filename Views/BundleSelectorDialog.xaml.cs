using DieselBundleViewer.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace DieselBundleViewer.Views
{
    /// <summary>
    /// Interaction logic for BundleSelectorDialog
    /// </summary>
    public partial class BundleSelectorDialog : UserControl
    {
        FixListView fix;
        public BundleSelectorDialog()
        {
            InitializeComponent();
            fix = new FixListView();
        }
        private void ListPreviewKeydown(object sender, KeyEventArgs e)
        {
            fix.ListPreviewKeydown(sender, e);
        }

        private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fix.ListSelectionChanged(sender, e);
        }

        private void ListPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            fix.ListPreviewMouseDown(sender, e);
        }

        private void ListItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            fix.ListItemPreviewMouseDown(sender, e);
        }
    }
}
