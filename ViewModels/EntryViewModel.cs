using DieselBundleViewer.Models;
using DieselBundleViewer.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DieselBundleViewer.ViewModels
{
    public class EntryViewModel : BindableBase
    {
        public IEntry Owner { get; set; }

        public string Icon {
            get {
                if (Owner == null || Owner is FolderEntry)
                    return "/Assets/folder.png";

                return Owner.Type switch
                {
                    "texture" => "/Assets/image.png",
                    "lua" => "/Assets/lua.png",
                    "movie" => "/Assets/video.png",
                    "font" => "/Assets/font.png",
                    _ => "/Assets/file.png",
                };
            }
        }

        public string Name => Owner.Name;
        public string Type => Owner.Type;
        public string Size
        {
            get
            {
                uint size = Owner.Size;
                string str_size;
                if (Owner is FolderEntry)
                    return "";
                else if (size < 1024)
                    str_size = size.ToString() + " B";
                else
                    str_size = string.Format("{0:n0}", size / 1024) + " KB";

                return str_size;
            }
        }
        private bool isSelected;

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public MainWindowViewModel ParentWindow { get; set; }

        public DelegateCommand OnDoubleClick { get; }
        public DelegateCommand<MouseButtonEventArgs> OnClick { get; }
        public EntryViewModel(MainWindowViewModel parentWindow, IEntry owner)
        {
            Owner = owner;
            ParentWindow = parentWindow;
            OnDoubleClick = new DelegateCommand(OnDoubleClickExec);
            OnClick = new DelegateCommand<MouseButtonEventArgs>(OnClickExec);
        }

        void OnClickExec(MouseButtonEventArgs e)
        {
            ParentWindow.OnClick();
        }

        void OnDoubleClickExec()
        {
            if (Owner is FolderEntry)
                ParentWindow.Navigate(Owner.EntryPath);
            else if (Owner is FileEntry)
                ParentWindow.FileManager.ViewFile((FileEntry)Owner);
        }
    }
}
