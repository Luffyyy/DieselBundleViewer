using DieselBundleViewer.Models;
using DieselBundleViewer.Services;
using DieselBundleViewer.Views;
using DieselEngineFormats.Bundle;
using DieselEngineFormats.Utils;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DieselBundleViewer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region properties
        private string _title = "Diesel Bundle Viewer";
        private PackageDatabase db;
        private TreeEntryViewModel Root { get; set; }

        private string _CurrentDir;
        private string _Status;

        public string Title{ get => _title; set => SetProperty(ref _title, value); }
        public string AssetsDir { get; set; }
        public string Status { get => _Status; set => SetProperty(ref _Status, value); }

        public Dictionary<Idstring, PackageHeader> PackageHeaders;

        Dictionary<uint, FileEntry> FileEntries { get; set; }

        public ObservableCollection<EntryViewModel> ToRender { get; set; }
        public ObservableCollection<TreeEntryViewModel> FoldersToRender { get; set; }

        public string CurrentDir { 
            get => _CurrentDir;
            set => SetDir(value, true);
        }

        public void SetDir(string dir, bool clearForward)
        {
            SetProperty(ref _CurrentDir, dir, "CurrentDir");
            if (clearForward)
                BackDirs.Clear();
            DirChanged();
        }

        public void DirChanged()
        {
            BackDir.RaiseCanExecuteChanged();
            ForwardDir.RaiseCanExecuteChanged();
            RenderNewItems();
        }

        public Stack<string> BackDirs = new Stack<string>();
        public DelegateCommand OpenFileDialog { get; }
        public DelegateCommand OpenAboutDialog { get; }
        public DelegateCommand OpenSettingsDialog { get; }
        public DelegateCommand ForwardDir { get; }
        public DelegateCommand BackDir { get; }
        public DelegateCommand<string> SetViewStyle { get; }

        private Point DragStartLocation;
        private bool Dragging;

        public TempFileManager FileManager { get; set; }

        private UserControl entriesStyle;

        public UserControl EntriesStyle { get => entriesStyle; set => SetProperty(ref entriesStyle, value); }

        #endregion

        public MainWindowViewModel(IDialogService dialogService)
        {
            EntriesStyle = new EntryListView();
            Utils.CurrentDialogService = dialogService;
            OpenFileDialog = new DelegateCommand(OpenFileDialogExec);
            BackDir = new DelegateCommand(BackDirExec, ()=>!string.IsNullOrEmpty(CurrentDir));
            ForwardDir = new DelegateCommand(ForwardDirExec, ()=>BackDirs.Count > 0);
            SetViewStyle = new DelegateCommand<string>(SetViewStyleExec);

            CurrentDir = "";
            PackageHeaders = new Dictionary<Idstring, PackageHeader>();
            ToRender = new ObservableCollection<EntryViewModel>();
            FoldersToRender = new ObservableCollection<TreeEntryViewModel>();

            ToRender.Add(new EntryViewModel(this, new FileEntry { Name = "test" }));

            FileManager = new TempFileManager();

            Status = "Start by opening a blb file. Press 'Open' and navigate to the assets directory of the game";

            Utils.OnMouseMoved += OnMouseMoved;
            OpenAboutDialog = new DelegateCommand(() =>
            {
                dialogService.ShowDialog("AboutDialog", new DialogParameters(), r => { });
            });
            OpenSettingsDialog = new DelegateCommand(() =>
            {
                dialogService.ShowDialog("SettingsDialog", new DialogParameters(), r => { });
            });
        }

        void SetViewStyleExec(string style)
        {
            if (style == "grid")
                EntriesStyle = new EntryGridView();
            else
                EntriesStyle = new EntryListView();
        }

        public void OnMouseMoved(Point pos)
        {
            Point diff = new Point(pos.X - DragStartLocation.X, pos.Y - DragStartLocation.Y);
            if (Dragging)
            {
                if (Math.Abs(diff.X) > 8 && Math.Abs(diff.Y) > 8)
                {
                    Debug.Print("Begin Drag!");
                    DragDropController controller = new DragDropController(false);
                    foreach(EntryViewModel vm in ToRender)
                    {
                        if (vm.IsSelected)
                        {
                            Debug.Print("Selected!");
                            controller.DoDragDrop(vm.Owner);
                        }
                    }
                    Dragging = false;
                }
            }
        }

        //Called from View.
        public void OnClick()
        {
            DragStartLocation = Utils.MousePos;
            Dragging = true;
        }

        public void OnRelease()
        {
            Dragging = false;
        }

        public async void OpenFileDialogExec()
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Bundle Database File (*.blb)|*.blb" };
            var result = ofd.ShowDialog();
            if (result == true)
                await OpenBLBFile(ofd.FileName);
        }

        public async Task OpenBLBFile(string filePath)
        {
            PackageHeaders.Clear();

            await Task.Run(() =>
            {
                Root = new TreeEntryViewModel(this, new FolderEntry { EntryPath = "assets", Name = "assets" });

                Status = "Preparing to open blb file...";
                AssetsDir = Path.GetDirectoryName(filePath);
                Status = "Reading blb file";
                Console.WriteLine("one");
                db = new PackageDatabase(filePath);
                General.LoadHashlist(AssetsDir, db);
                Status = "Getting bundle headers";
                Console.WriteLine("two");

                List<string> Files = Directory.EnumerateFiles(AssetsDir, "*.bundle").ToList();

                FileEntries = DatabaseEntryToFileEntry(db.GetDatabaseEntries());
                Console.WriteLine("three");

                List<string> FilterFiles = new List<string>();
                for (int i = 0; i < Files.Count; i++)
                {
                    string file = Files[i];
                    if (!file.EndsWith("_h.bundle"))
                        FilterFiles.Add(file);
                }
                for (int i = 0; i < FilterFiles.Count; i++)
                {
                    string file = FilterFiles[i];

                    Status = string.Format("Loading bundle {0} {1}/{2}", file, i, FilterFiles.Count);

                    PackageHeader bundle = new PackageHeader();
                    if (!bundle.Load(file))
                        continue;

                    foreach (PackageFileEntry be in bundle.Entries)
                        if (FileEntries.ContainsKey(be.ID))
                            FileEntries[be.ID].AddBundleEntry(be);

                    PackageHeaders.Add(bundle.Name, bundle);
                }
            });

            GC.Collect();

            Status = "Done";

            RenderNewItems();
            FoldersToRender.Clear();
            FoldersToRender.Add(Root);
        }

        public void ForwardDirExec()
        {
            if (BackDirs.Count > 0)
                SetDir(Utils.CombineDir(CurrentDir, BackDirs.Pop()), false);
        }

        public void BackDirExec()
        {
            if (CurrentDir != "" && CurrentDir != "/")
            {
                BackDirs.Push(Path.GetFileName(CurrentDir));
                SetDir(Utils.GetDirectory(CurrentDir), false);
            }
            else
                Debug.Print("Should not be called " + CurrentDir);
        }

        public void RenderNewItems()
        {
            if (ToRender == null)
                return;
            ToRender.Clear();

            // RenderNewItemsFolders(Root);
            var children = Root.Owner.GetEntriesByDirectory(CurrentDir);

            bool disEmpty = Settings.Data.DisplayEmptyFiles;
            foreach (var entry in children)
            {
                if(!(entry is FileEntry) || disEmpty || ((entry as FileEntry).Size > 0))
                    ToRender.Add(new EntryViewModel(this, entry));
            }

            /*Items.Sort((a,b) => {
                var ret = (a is FolderEntry).CompareTo(b is FolderEntry);
                Debug.Write(ret);
                return ret == 0 ? a.Name.CompareTo(b.Name) : ret;
            });*/

        }

        public Dictionary<uint, FileEntry> DatabaseEntryToFileEntry(List<DatabaseEntry> entries)
        {
            Dictionary<uint, FileEntry> fileEntries = new Dictionary<uint, FileEntry>();
            foreach (DatabaseEntry ne in entries)
            {
                FileEntry fe = new FileEntry(ne, db, this);
                Root.Owner.AddFileEntry(fe);
                fileEntries.Add(ne.ID, fe);
            }
            return fileEntries;
        }
    }
}
