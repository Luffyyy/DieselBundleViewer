using DieselBundleViewer.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DieselBundleViewer.ViewModels
{
    public class TreeEntryViewModel : BindableBase
    {
        public FolderEntry Owner { get; set; }
        public string Icon => "/Assets/folder.png";
        public string Name => Owner.Name;
        public bool IsExpanded { get; set; }

        private bool isSelected;
        public bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }

        public MainWindowViewModel ParentWindow { get; set; }
        public DelegateCommand<MouseButtonEventArgs> OnClick { get; }

        public List<TreeEntryViewModel> Children
        {
            get
            {
                List<TreeEntryViewModel> ChildrenEntries = new List<TreeEntryViewModel>();
                foreach (IEntry val in Owner.Children.Values)
                {
                    if (val is FolderEntry)
                        ChildrenEntries.Add(new TreeEntryViewModel(ParentWindow, (FolderEntry)val));
                }
                return ChildrenEntries;
            }
        }

        public TreeEntryViewModel(MainWindowViewModel parentWindow, FolderEntry owner)
        {
            Owner = owner;
            ParentWindow = parentWindow;
            OnClick = new DelegateCommand<MouseButtonEventArgs>(OnClickExec);
            if (Owner.Name == "assets")
            {
                IsExpanded = true;
                IsSelected = true;
            }
        }

        void OnClickExec(MouseButtonEventArgs e)
        {
            ParentWindow.OnClick();
            ParentWindow.Navigate(Owner.EntryPath);
            IsSelected = true;
        }
    }
}
