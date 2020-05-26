using DieselBundleViewer.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

/// <summary>
/// This is generally needed because the extended selection mode in lists doesn't really work as one would expect.
/// Instead of selecting everything via Ctrl+A, it selects only what is visible. This is fine in terms of virtualization,
/// but awful in pratice and so we obviously need to fix.
/// </summary>
namespace DieselBundleViewer.ViewModels
{
    /// <summary>
    /// A class to have a base for list item based items so we can deal with them without being too specific
    /// </summary>
    public class ListItemViewModelBase : BindableBase
    {
        protected bool isSelected;
        public virtual bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }
    }

    public class FixListView
    {
        ListBox lastList;

        public void ListPreviewKeydown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.A))
            {
                ListBox list = (ListBox)sender;
                foreach (ListItemViewModelBase item in list.Items)
                {
                    item.IsSelected = true;
                }
            }
        }

        public void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                ListBox list = (ListBox)sender;

                var items = list.SelectedItems;
                if (items.Count == 0)
                    return;

                var firstIndex = list.SelectedIndex;
                var lastItem = (ListItemViewModelBase)items[items.Count - 1];

                if (lastItem != null)
                {
                    int lastIndex = list.Items.IndexOf(lastItem);

                    if (firstIndex > lastIndex)
                    {
                        int temp = firstIndex;
                        firstIndex = list.Items.IndexOf(items[1]);
                        lastIndex = temp;
                    }

                    for (int i = firstIndex; i < lastIndex + 1; i++)
                    {
                        var item = (list.Items[i] as ListItemViewModelBase);
                        if (!item.IsSelected)
                            item.IsSelected = true;
                    }
                }
            }
        }

        public void ListItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (lastList != null)
            {
                if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Utils.CurrentWindow.Dragging)
                {
                    foreach (ListItemViewModelBase item in lastList.Items)
                    {
                        item.IsSelected = false;
                    }
                }
            }
        }

        public void ListPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (lastList == null)
                lastList = (ListBox)sender;
        }

        public void ListItemPreviewMouseMouseMove(object sender, MouseEventArgs e)
        {
            //Fixes a bug where drag and drop would take the file near instead.
            if (Utils.CurrentWindow.Dragging)
                e.Handled = true;
        }
    }
}
