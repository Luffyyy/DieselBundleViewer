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

        // https://docs.microsoft.com/en-us/dotnet/framework/wpf/controls/how-to-sort-a-gridview-column-when-a-header-is-clicked
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void SelectedEntries_Click_1(object sender, RoutedEventArgs e)
        {
            ListSortDirection direction;

            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                        direction = ListSortDirection.Ascending;
                    else
                        direction = _lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    else
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                        _lastHeaderClicked.Column.HeaderTemplate = null;

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView((DataContext as MainWindowViewModel).ToRender);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
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
