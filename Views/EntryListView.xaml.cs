using DieselBundleViewer.Objects;
using DieselBundleViewer.Services;
using DieselBundleViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DieselBundleViewer.Views
{
    /// <summary>
    /// Interaction logic for ListView.xaml
    /// </summary>
    public partial class EntryListView : UserControl
    {
        private FixListView fix;
        public EntryListView()
        {
            fix = new FixListView();
            InitializeComponent();
        }

        private GridViewColumnHeader lastHeaderClicked = null;

        private void SelectedEntries_Click_1(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;

                    PageData page = Utils.CurrentWindow.CurrentPage.Value;
                    if (Enum.TryParse((string)headerClicked.Column.Header, out Sorting sort))
                        page.SortBy = sort;

                    page.Ascending = !page.Ascending;

                    lastHeaderClicked = headerClicked;

                    Utils.CurrentWindow.RenderNewItems();
                }
            }
            e.Handled = true;
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
