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

                Console.WriteLine(Owner.Type);
                return Owner.Type switch
                {
                    "texture" => "/Assets/image.png",
                    "lua" => "/Assets/lua.png",
                    "movie" => "/Assets/video.png",
                    _ => "/Assets/file.png",
                };
            }
        }

        public string Name => Owner.Name;
        public string Type => Owner.Type;
        public string Size => Owner.SizeStr;

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
            Console.WriteLine("ok");

            if (Owner is FolderEntry)
                ParentWindow.CurrentDir = Owner.EntryPath;
            else if (Owner is FileEntry)
                ParentWindow.FileManager.ViewFile((FileEntry)Owner);
        }
    }
}
