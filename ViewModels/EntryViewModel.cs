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

        public override string Name => Owner.Name;
        public string EntryPath => Owner.EntryPath;
        public string Type => Owner.Type;
        public uint Size => (Owner is FolderEntry) ? 0 : Owner.Size;
        public string FriendlySize => (Owner is FolderEntry) ? "" : Utils.FriendlySize(Owner.Size);

        public bool FileLocationVis => Utils.CurrentWindow.CurrentPage.Value.IsSearch && SingleSelectionVis;

        public bool ConvertSaveVis
        {
            get
            {
                if (Owner is FileEntry entry && SingleSelectionVis)
                {
                    string typ = Definitions.TypeFromExtension(entry.ExtensionIds.ToString());
                    return ScriptActions.Converters.ContainsKey(typ);
                }
                return false;
            }
        }
        public bool SingleSelectionVis => !Utils.CurrentWindow.IsSelectingMultiple();

        public override bool IsSelected {
            get => isSelected;
            set {
                bool wasSelected = isSelected;
                SetProperty(ref isSelected, value);
                if(wasSelected != value)
                    Utils.CurrentWindow.UpdateFileStatus();
            }
        }

        public DelegateCommand OnDoubleClick { get; }
        public DelegateCommand<MouseButtonEventArgs> OnClick { get; }
        public DelegateCommand OpenFileInfo { get; }
        public DelegateCommand OpenFileLocation { get; }
        public DelegateCommand<string> SaveAs { get; }

        public EntryViewModel(IEntry owner)
        {
            Owner = owner;
            OnDoubleClick = new DelegateCommand(OnDoubleClickExec);
            OnClick = new DelegateCommand<MouseButtonEventArgs>(OnClickExec);
            OpenFileInfo = new DelegateCommand(OpenFileInfoExec);
            OpenFileLocation = new DelegateCommand(OpenFileLocationExec);
            SaveAs = new DelegateCommand<string>(SaveAsExec);
        }

        void OpenFileLocationExec()
        {
            Utils.CurrentWindow.Navigate(Owner.Parent.EntryPath);
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
            Utils.CurrentWindow.OnClick();
        }

        void OnDoubleClickExec()
        {
            if (Owner is FolderEntry)
                Utils.CurrentWindow.Navigate(Owner.EntryPath);
            else if (Owner is FileEntry entry)
                FileManager.ViewFile(entry);
        }

        void SaveAsExec(string convert)
        {
            List<EntryViewModel> selection = Utils.CurrentWindow.SelectedEntries();
            string currentDir = Utils.CurrentWindow.CurrentDir;
            if(selection.Count == 1)
            {
                if (convert == "True")
                    FileManager.SaveFileConvert((FileEntry)Owner, currentDir);
                else if (Owner is FileEntry entry)
                    FileManager.SaveFileAs(entry, currentDir);
                else if (Owner is FolderEntry fEntry)
                    FileManager.SaveMultiple(fEntry.GetAllChildren(), currentDir);
            } else
            {
                List<IEntry> entries = new List<IEntry>();
                foreach(var entry in selection)
                {
                    if (entry.Owner is FileEntry file)
                        entries.Add(file);
                    else if (entry.Owner is FolderEntry folder)
                        entries.AddRange(folder.GetAllChildren());
                }
                FileManager.SaveMultiple(entries, currentDir);
            }
        }
    }
}
