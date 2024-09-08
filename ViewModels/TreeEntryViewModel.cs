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

        public bool IsExpanded { get; set; } = false;

        private bool isSelected;
        public bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }

        public MainWindowViewModel ParentWindow { get; set; }
        public DelegateCommand<MouseButtonEventArgs> OnClick { get; }

        private List<TreeEntryViewModel> children;
        public List<TreeEntryViewModel> Children { 
            get {
                if(children == null)
                {
                    children = new List<TreeEntryViewModel>();
                    foreach (IEntry val in Owner.Children.Values)
                    {
                        if (val is FolderEntry folder && folder.HasVisibleFiles())
                            children.Add(new TreeEntryViewModel(ParentWindow, folder));
                    }
                }
                return children;
            }
            set => SetProperty(ref children, value); 
        }

        //There is a slight delay when opening units in payday 2. Need to maybe optimize it further.
        public void CheckExpands()
        {
            string currentDir = ParentWindow.CurrentDir;
            bool needsCheck = true;
            bool wasExpanded = IsExpanded;

            foreach (var child in Children)
            {
                child.CheckExpands();
                if(child.IsExpanded)
                {
                    needsCheck = false;
                    IsExpanded = true;
                }
            }

            if(needsCheck)
                IsExpanded = currentDir.StartsWith(Owner.EntryPath);

            if(wasExpanded != IsExpanded)
                RaisePropertyChanged(nameof(IsExpanded));

            if(isSelected != IsSelected)
                IsSelected = currentDir == Owner.EntryPath;
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
            //ParentWindow.OnClick();
            ParentWindow.Navigate(Owner.EntryPath);
            IsSelected = true;
        }
    }
}
