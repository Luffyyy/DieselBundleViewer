using DieselBundleViewer.Models;
using DieselBundleViewer.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace DieselBundleViewer.ViewModels
{
    public class EntryViewModel : ListItemViewModelBase
    {
        public IEntry Owner { get; set; }

        public bool IsFolder => Owner is FolderEntry;

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
        public string EntryPath => Owner.EntryPath;
        public string Type => Owner.Type;
        public string Size => (Owner is FolderEntry) ? "" : Utils.FriendlySize(Owner.Size);

        public bool FileLocationVis => ParentWindow.CurrentPage.Value.IsSearch;
        public bool ConvertSaveVis
        {
            get
            {
                if (Owner is FileEntry entry)
                {
                    string typ = Definitions.TypeFromExtension(entry.ExtensionIds.ToString());
                    return ScriptActions.Converters.ContainsKey(typ);
                }
                return false;
            }
        }

        public override bool IsSelected {
            get => isSelected;
            set {
                bool wasSelected = isSelected;
                SetProperty(ref isSelected, value);
                if(wasSelected != value)
                    ParentWindow.UpdateFileStatus();
            }
        }

        public MainWindowViewModel ParentWindow { get; set; }

        public DelegateCommand OnDoubleClick { get; }
        public DelegateCommand<MouseButtonEventArgs> OnClick { get; }
        public DelegateCommand OpenFileInfo { get; }
        public DelegateCommand OpenFileLocation { get; }
        public DelegateCommand<string> SaveAs { get; }

        public EntryViewModel(MainWindowViewModel parentWindow, IEntry owner)
        {
            Owner = owner;
            ParentWindow = parentWindow;
            OnDoubleClick = new DelegateCommand(OnDoubleClickExec);
            OnClick = new DelegateCommand<MouseButtonEventArgs>(OnClickExec);
            OpenFileInfo = new DelegateCommand(OpenFileInfoExec);
            OpenFileLocation = new DelegateCommand(OpenFileLocationExec);
            SaveAs = new DelegateCommand<string>(SaveAsExec);
        }

        void OpenFileLocationExec()
        {
            ParentWindow.Navigate(Owner.Parent.EntryPath);
        }

        void OpenFileInfoExec()
        {
            var pms = new Prism.Services.Dialogs.DialogParameters
            {
                { "Entry", this }
            };
            Utils.ShowDialog("PropertiesDialog", pms);
        }

        void OnClickExec(MouseButtonEventArgs e)
        {
            ParentWindow.OnClick();
        }

        void OnDoubleClickExec()
        {
            if (Owner is FolderEntry)
                ParentWindow.Navigate(Owner.EntryPath);
            else if (Owner is FileEntry entry)
                FileManager.ViewFile(entry);
        }

        void SaveAsExec(string convert)
        {
            if (convert == "True")
                FileManager.SaveFileConvert((FileEntry)Owner);
            else if (Owner is FileEntry entry)
                FileManager.SaveFileAs(entry);
            else if (Owner is FolderEntry fEntry)
                FileManager.SaveFolder(fEntry);
        }
    }
}
