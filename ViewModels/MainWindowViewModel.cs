using AdonisUI;
using DieselBundleViewer.Models;
using DieselBundleViewer.Objects;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DieselBundleViewer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Properties / Fields
        private string _title = "Diesel Bundle Viewer";
        private PackageDatabase db;
        public TreeEntryViewModel Root { get; set; }

        public string Title { get => _title; set => SetProperty(ref _title, value); }

        public string AssetsDir { get; set; }

        private string status;
        public string Status { get => status; set => SetProperty(ref status, value); }

        private string fileStatus;
        public string FileStatus { get => fileStatus; set => SetProperty(ref fileStatus, value); }

        private int gridViewScale = 32;
        public int GridViewScale { get => gridViewScale; set => SetProperty(ref gridViewScale, value); }

        public Dictionary<Idstring, PackageHeader> PackageHeaders;

        public Dictionary<uint, FileEntry> FileEntries { get; set; }

        //Used by scripts.
        public Dictionary<Tuple<Idstring, Idstring, Idstring>, FileEntry> RawFiles;

        public ObservableCollection<EntryViewModel> ToRender { get; set; }
        public ObservableCollection<TreeEntryViewModel> FoldersToRender { get; set; }
        public List<Script> Scripts => ScriptActions.Scripts;
        public bool ScriptsVisible => Scripts.Count > 0;
        public ObservableCollection<string> RecentFiles { get; set; }
        public bool RecentFilesVis => RecentFiles.Count > 0;

        public List<Idstring> Bundles { get; set; }
        public List<Idstring> SelectedBundles { get; set; }

        public LinkedList<PageData> Pages = new LinkedList<PageData>();
        public LinkedListNode<PageData> CurrentPage;
        public string CurrentDir { get => CurrentPage?.Value.Path; set => Navigate(value); }

        public DelegateCommand OpenFileDialog { get; }
        public DelegateCommand<string> OpenFile { get; }
        public DelegateCommand OpenAboutDialog { get; }
        public DelegateCommand OpenSettingsDialog { get; }
        public DelegateCommand OpenFindDialog { get; }
        public DelegateCommand OpenBundleSelectorDialog { get; }
        public DelegateCommand ForwardDir { get; }
        public DelegateCommand BackDir { get; }
        public DelegateCommand OnKeyDown { get; }
        public DelegateCommand CloseBLB { get; }
        public DelegateCommand<string> SetViewStyle { get; }
        public DelegateCommand OpenHowToUse { get; }
        public DelegateCommand ExtractAll { get; }

        private Point DragStartLocation;
        private bool Dragging;

        private CancellationTokenSource cancelLastTask;

        private UserControl entriesStyle;
        public UserControl EntriesStyle { get => entriesStyle; set => SetProperty(ref entriesStyle, value); }

        #endregion

        public MainWindowViewModel(DialogService dialogService)
        {
            Utils.CurrentDialogService = dialogService;
            Utils.CurrentWindow = this;

            //Lists and stuff for the bundles/files/etc
            PackageHeaders = new Dictionary<Idstring, PackageHeader>();
            Bundles = new List<Idstring>();
            ToRender = new ObservableCollection<EntryViewModel>();
            FoldersToRender = new ObservableCollection<TreeEntryViewModel>();
            SelectedBundles = new List<Idstring>();
            RawFiles = new Dictionary<Tuple<Idstring, Idstring, Idstring>, FileEntry>();
            RecentFiles = new ObservableCollection<string>(Settings.Data.RecentFiles);

            //Commands / Events
            OpenFileDialog = new DelegateCommand(OpenFileDialogExec);
            OpenFile = new DelegateCommand<string>(OpenFileExec);
            CloseBLB = new DelegateCommand(CloseBLBExec);
            OpenBundleSelectorDialog = new DelegateCommand(OpenBundleSelectorDialogExec, () => Root != null);
            OpenFindDialog = new DelegateCommand(OpenFindDialogExec, () => Root != null);
            BackDir = new DelegateCommand(BackDirExec, ()=> CurrentPage?.Previous != null);
            ForwardDir = new DelegateCommand(ForwardDirExec, ()=> CurrentPage?.Next != null);
            OnKeyDown = new DelegateCommand(OnKeyDownExec);
            SetViewStyle = new DelegateCommand<string>(style => SetViewStyleExec(style, true));
            OpenAboutDialog = new DelegateCommand(() => Utils.ShowDialog("AboutDialog", null, true));
            OpenSettingsDialog = new DelegateCommand(() => Utils.ShowDialog("SettingsDialog", r => UpdateSettings()));
            OpenHowToUse = new DelegateCommand(() => {
                Process.Start(new ProcessStartInfo("https://github.com/Luffyyy/DieselBundleViewer/wiki/How-to-Use") { UseShellExecute = true });
            });
            ExtractAll = new DelegateCommand(ExtractAllExec, () => Root != null);

            Utils.OnMouseMoved += OnMouseMoved;

            //Set status to default
            CloseBLBExec();

            //Testing
            //ToRender.Add(new EntryViewModel(this, new FileEntry { Name = "test" }));

            //Grid or list?
            EntriesStyle = new EntryListView();

            UpdateSettings();
        }

        #region Commands
        void OnKeyDownExec()
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (Keyboard.IsKeyDown(Key.F) && OpenFindDialog.CanExecute())
                    OpenFindDialog.Execute();
                else if (Keyboard.IsKeyDown(Key.O) && OpenFileDialog.CanExecute())
                    OpenFileDialog.Execute();
                else if(Keyboard.IsKeyDown(Key.B) && OpenBundleSelectorDialog.CanExecute())
                    OpenBundleSelectorDialog.Execute();
            }
        }

        void UpdateSettings()
        {
            ResourceLocator.SetColorScheme(App.Current.Resources, 
                Settings.Data.DarkMode ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);
            RenderNewItems();
        }

        void OpenFindDialogExec()
        {
            if (Utils.DialogOpened("FindDialog"))
                return;

            Utils.ShowDialog("FindDialog", r => {
                string search = r.Parameters.GetValue<string>("Search");
                if (!string.IsNullOrEmpty(search))
                {
                    bool useRegex = FindDialogViewModel.UseRegex;
                    bool matchWord = FindDialogViewModel.MatchWord;
                    bool fullPath = FindDialogViewModel.FullPath;
                    Navigate(new PageData($"Search Results: '{search}' (Use Regex: {useRegex}, Match Word: {matchWord}, Full Path: {fullPath})")
                    {
                        IsSearch = true,
                        Search = search,
                        FullPath = fullPath,
                        UseRegex = useRegex,
                        MatchWord = matchWord
                    });
                }
            });
        }

        void OpenBundleSelectorDialogExec()
        {
            DialogParameters pms = new DialogParameters
            {
                { "Bundles", Bundles }
            };
            Utils.ShowDialog("BundleSelectorDialog", pms, r =>
            {
                var selectedBundles = pms.GetValue<List<Idstring>>("SelectedBundles");
                if(selectedBundles != null)
                    SelectedBundles = selectedBundles;
                RenderNewItems();
            }, true);
        }

        void SetViewStyleExec(string style, bool resetScale=false)
        {
            bool isGrid = style == "grid";
            if (resetScale)
                GridViewScale = isGrid ? 64 : 32;
            if (isGrid)
                EntriesStyle = new EntryGridView();
            else
                EntriesStyle = new EntryListView();
        }

        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                e.Handled = true;

                int scale = GridViewScale;

                if(e.Delta > 0)
                    scale += 8;
                else if(e.Delta < 0)
                    scale -= 8;

                GridViewScale = Math.Clamp(scale, 32, 128);
                if (GridViewScale == 32)
                {
                    if(EntriesStyle is EntryGridView)
                        SetViewStyleExec("list");
                }
                else if(EntriesStyle is EntryListView)
                    SetViewStyleExec("grid");
            }
        }

        public void OnMouseMoved(Point pos)
        {
            Point diff = new Point(pos.X - DragStartLocation.X, pos.Y - DragStartLocation.Y);
            if (Dragging)
            {
                if (Math.Abs(diff.X) > 8 && Math.Abs(diff.Y) > 8)
                {
                    DragDropController controller = new DragDropController(Settings.Data.ExtractFullDir);
                    var selected = new List<IEntry>();
                    foreach(var vm in ToRender)
                    {
                        if (vm.IsSelected)
                            selected.Add(vm.Owner);
                    }
                    controller.DoDragDrop(selected);
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
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Bundle Database File (*.blb)|*.blb"};
            
            //Here we try to default to 2 very possible directories of PD2 which most users will use it for.
            if(!RecentFilesVis)
            {
                string possiblePath = @"C:\Program Files (x86)\Steam\steamapps\common\PAYDAY 2\assets";
                bool found = Directory.Exists(possiblePath);
                if (!found)
                {
                    possiblePath = @"D:\SteamLibrary\steamapps\common\PAYDAY 2\assets";
                    found = Directory.Exists(possiblePath);
                }

                if (found)
                    ofd.InitialDirectory = possiblePath;
            }
            
            var result = ofd.ShowDialog();
            if (result == true)
            {
                string fileName = ofd.FileName;

                //If the file already exists in the recents, bump it.
                if (RecentFiles.Contains(fileName))
                    RecentFiles.Remove(fileName);

                RecentFiles.Add(fileName);

                Settings.Data.RecentFiles = RecentFiles.ToList();
                Settings.SaveSettings();

                await OpenBLBFile(fileName);
            }
        }

        public async void OpenFileExec(string fileName)
        {
            await OpenBLBFile(fileName);
        }

        public void ExtractAllExec()
        {
            if(Root != null)
                FileManager.SaveFolder(Root.Owner);
        }

        public void CloseBLBExec()
        {
            if (cancelLastTask != null)
                cancelLastTask.Cancel();

            Status = "Start by opening a blb file. Press 'File->Open' and navigate to the assets directory of the game";
            Pages.Clear();
            CurrentDir = "";
            FileStatus = "";

            if (Root != null)
            {
                Bundles = null;
                FileEntries = null;
                Root = null;
                db = null;
                PackageHeaders.Clear();
                ToRender.Clear();
                FoldersToRender.Clear();
                RawFiles.Clear();
                GC.Collect();
                OpenFindDialog.RaiseCanExecuteChanged();
                ExtractAll.RaiseCanExecuteChanged();
                OpenBundleSelectorDialog.RaiseCanExecuteChanged();
            }
        }
        #endregion

        public async Task OpenBLBFile(string filePath)
        {
            CloseBLBExec();

            CancellationTokenSource CancelSource = new CancellationTokenSource();
            cancelLastTask = CancelSource;

            await Task.Run(() =>
            {
                if (CancelSource.IsCancellationRequested)
                    return;

                //Create the root tree entry, here all other folders will reside.
                Root = new TreeEntryViewModel(this, new FolderEntry { EntryPath = "", Name = "assets" });

                Status = "Preparing to open blb file...";
                AssetsDir = Path.GetDirectoryName(filePath);
                Status = "Reading blb file";

                db = new PackageDatabase(filePath);
                General.LoadHashlist(AssetsDir, db);
                Status = "Getting bundle headers";

                List<string> Files = Directory.EnumerateFiles(AssetsDir, "*.bundle").ToList();

                FileEntries = DatabaseEntryToFileEntry(db.GetDatabaseEntries());

                List<string> FilterFiles = new List<string>();
                for (int i = 0; i < Files.Count; i++)
                {
                    if (CancelSource.IsCancellationRequested)
                        return;

                    string file = Files[i];
                    if (!file.EndsWith("_h.bundle"))
                        FilterFiles.Add(file);
                }
                for (int i = 0; i < FilterFiles.Count; i++)
                {
                    if (CancelSource.IsCancellationRequested)
                        return;

                    string file = FilterFiles[i];

                    Status = string.Format("Loading bundle {0} {1}/{2}", file, i, FilterFiles.Count);

                    PackageHeader bundle = new PackageHeader();
                    if (!bundle.Load(file))
                        continue;

                    foreach (PackageFileEntry be in bundle.Entries)
                    {
                        if (CancelSource.IsCancellationRequested)
                            return;

                        if (FileEntries.ContainsKey(be.ID))
                        {
                            FileEntry fileEntry = FileEntries[be.ID];
                            fileEntry.AddBundleEntry(be);
                            FolderEntry folderEntry = fileEntry.Parent;
                        }
                    }

                    PackageHeaders.Add(bundle.Name, bundle);
                }

                GC.Collect();
            });

            if (CancelSource.IsCancellationRequested)
                return;

            OpenFindDialog.RaiseCanExecuteChanged();
            ExtractAll.RaiseCanExecuteChanged();
            OpenBundleSelectorDialog.RaiseCanExecuteChanged();

            Status = "Done";

            Bundles = PackageHeaders.Keys.ToList();
            FoldersToRender.Clear();
            FoldersToRender.Add(Root);

            //Finally, render the items.
            RenderNewItems();
        }

        public void UpdateFileStatus()
        {
            string newStatus = ToRender.Count + " Items |";
            uint totalSize = 0;
            uint totalSelected = 0;
            string size = "";

            foreach (var entry in ToRender)
            {
                if (entry.IsSelected)
                {
                    if (entry.IsFolder)
                        totalSize += (entry.Owner as FolderEntry).GetTotalSize();
                    else
                        totalSize += entry.Owner.Size;

                    totalSelected++;
                }
            }

            if (totalSize > 0)
                size = Utils.FriendlySize(totalSize);

            FileStatus = newStatus + $" {totalSelected} items selected {size} |";
        }

        public void SetDir(LinkedListNode<PageData> dir)
        {
            SetProperty(ref CurrentPage, dir, "CurrentDir");
            DirChanged();
        }

        public void DirChanged()
        {
            BackDir.RaiseCanExecuteChanged();
            ForwardDir.RaiseCanExecuteChanged();
            RenderNewItems();
        }

        public void Navigate(string dir) => Navigate(new PageData(dir));
         
        public void Navigate(PageData dir)
        {
            if (CurrentPage != null && CurrentPage.Next != null)
            {
                var node = CurrentPage.Next;
                while(node != null) {
                    var next = node.Next;
                    Pages.Remove(node);
                    node = next;
                }
            }
            SetDir(Pages.AddLast(dir));
        }

        public void ForwardDirExec()
        {
            if (CurrentPage.Next != null)
                SetDir(CurrentPage.Next);
        }

        public void BackDirExec()
        {
            var next = CurrentPage.Previous;
            if (next != null)
                SetDir(next);
        }

        public void RenderNewItems()
        {
            if (ToRender == null || Root == null)
                return;
            ToRender.Clear();

            List<IEntry> children;

            PageData page = CurrentPage.Value;
            string search = page.Search;
            if (!string.IsNullOrEmpty(search))
            {
                children = Root.Owner.GetEntriesByConiditions(entry =>
                {
                    string searchUsing;
                    if (page.FullPath)
                        searchUsing = entry.EntryPath;
                    else
                        searchUsing = entry.Name;

                    if (SelectedBundles.Count > 0 && !entry.InBundles(SelectedBundles))
                        return false;
                    else if (page.UseRegex)
                        return Regex.IsMatch(searchUsing, search);
                    else if (page.MatchWord)
                        return searchUsing == search;
                    else
                        return searchUsing.Contains(search);
                });
            } else
                children = Root.Owner.GetEntriesByDirectory(CurrentDir);

            foreach (var entry in children)
            {
                if(SelectedBundles.Count == 0 || entry.InBundles(SelectedBundles))
                {
                    if(entry is FileEntry && (entry as FileEntry).HasData() || entry is FolderEntry && (entry as FolderEntry).HasVisibleFiles())
                        ToRender.Add(new EntryViewModel(this, entry));
                }
            }

            Root.CheckExpands();
            UpdateFileStatus();
        }

        public Dictionary<uint, FileEntry> DatabaseEntryToFileEntry(List<DatabaseEntry> entries)
        {
            Dictionary<uint, FileEntry> fileEntries = new Dictionary<uint, FileEntry>();
            foreach (DatabaseEntry ne in entries)
            {
                FileEntry fe = new FileEntry(ne, db, this);

                RawFiles.Add(new Tuple<Idstring, Idstring, Idstring>(fe.PathIds, fe.LanguageIds, fe.ExtensionIds), fe);
                Root.Owner.AddFileEntry(fe);
                fileEntries.Add(ne.ID, fe);
            }
            return fileEntries;
        }
    }
}
