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
        public EntryListView()
        {
            InitializeComponent();
        }

        // https://docs.microsoft.com/en-us/dotnet/framework/wpf/controls/how-to-sort-a-gridview-column-when-a-header-is-clicked
        GridViewColumnHeader _lastHeaderClicked = null;
        ListBox lastList;
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

        private void SelectedEntries_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.A))
            {

                ListBox list = (ListBox)sender;
                foreach(ListItemViewModelBase item in list.Items)
                {
                    item.IsSelected = true;
                }
            }
        }

        private void SelectedEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                ListBox list = (ListBox)sender;

                var items = list.SelectedItems;
                if (items.Count == 0)
                    return;

                var firstIndex = list.SelectedIndex;
                var lastItem = (ListItemViewModelBase)items[items.Count-1];

                if (lastItem != null)
                {
                    int lastIndex = list.Items.IndexOf(lastItem);

                    if (firstIndex > lastIndex)
                    {
                        int temp = firstIndex;
                        firstIndex = list.Items.IndexOf(items[1]);
                        lastIndex = temp;

                        Console.WriteLine("First index is bigger, swapping");
                    }

                    Console.WriteLine("{0} -> {1}", firstIndex, lastIndex);


                    for (int i=firstIndex; i<lastIndex+1; i++)
                    {
                        var item = (list.Items[i] as ListItemViewModelBase);
                        if (!item.IsSelected)
                            item.IsSelected = true;
                    }
                }
            }
        }

        private void ListViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(lastList != null)
            {
                if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    Console.WriteLine(lastList.Items);
                    foreach (ListItemViewModelBase item in lastList.Items)
                    {
                        item.IsSelected = false;
                    }
                }
            }
        }

        private void SelectedEntries_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (lastList == null)
                lastList = (ListBox)sender;
        }
    }
}
