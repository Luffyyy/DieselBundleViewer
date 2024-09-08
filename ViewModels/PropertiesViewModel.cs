using DieselBundleViewer.Models;
using DieselEngineFormats.Bundle;
using Prism.Dialogs;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using DieselBundleViewer.Services;

namespace DieselBundleViewer.ViewModels
{
    public class PackageFileViewModel(PackageFileEntry entry) : BindableBase
    {
        private PackageFileEntry _entry = entry;

        public string Name => _entry.PackageName.UnHashed;
        public string HashedName => _entry.PackageName.HashedString;
        public string Size => _entry.FileSize;
        public uint Address => _entry.Address;
        public int Length => _entry.Length;
    }

    public class PropertiesViewModel : DialogBase
    {
        public override string Title => entryVM.Name + " Properties";

        private EntryViewModel entryVM;

        public ObservableCollection<PackageFileViewModel> Bundles { get; }
        public bool FolderVisibility => (entryVM != null && entryVM.IsFolder);
        public bool FileVisibility => !FolderVisibility;

        public string FolderContains
        {
            get
            {
                if (entryVM == null || !entryVM.IsFolder)
                    return "";

                var children = (entryVM.Owner as FolderEntry).GetAllChildren();
                uint files = 0;
                uint folders = 0;
                foreach(var child in children)
                {
                    if (child is FileEntry)
                        files++;
                    else
                        folders++;
                }
                return $"{files} Files, {folders} Folders";
            }
        }
        public string Icon => entryVM?.Icon;
        public string Name => entryVM?.Name;
        public string Type => entryVM?.Type;
        public string Size { 
            get
            {
                if (entryVM == null)
                    return "";

                if (entryVM.IsFolder)
                    return Utils.FriendlySize((entryVM.Owner as FolderEntry).TotalSize);
                else
                    return entryVM?.FriendlySize;
            }
        }
        public string EntryPath => entryVM?.EntryPath;
        public string HashedName
        {
            get {
                if (entryVM == null || entryVM.Owner is FolderEntry)
                    return "";
                else
                    return (entryVM.Owner as FileEntry).PathIds.HashedString;
            }
        }

        public PropertiesViewModel() : base()
        {
            Bundles = new ObservableCollection<PackageFileViewModel>();
        }

        protected override void PostDialogOpened(IDialogParameters pms)
        {
            entryVM = pms.GetValue<EntryViewModel>("Entry");
            RaisePropertyChanged(nameof(FileVisibility));
            RaisePropertyChanged(nameof(FolderVisibility));
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(Icon));
            RaisePropertyChanged(nameof(Type));
            RaisePropertyChanged(nameof(Size));
            RaisePropertyChanged(nameof(EntryPath));
            RaisePropertyChanged(nameof(HashedName));
            RaisePropertyChanged(nameof(FolderContains));

            IEntry entry = entryVM.Owner;
            if(entry is FileEntry)
            {
                var bundles = (entry as FileEntry).BundleEntries;
                foreach(var pair in bundles)
                {
                    var e = new PackageFileViewModel(pair.Value);
                    Bundles.Add(e);
                }
            }
        }
    }
}
