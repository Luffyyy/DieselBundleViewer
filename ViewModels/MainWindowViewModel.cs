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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        public string Title { get => _title; set => SetProperty(ref _title, value); }

        public string AssetsDir { get; set; }

        private string _Status;
        public string Status { get => _Status; set => SetProperty(ref _Status, value); }

        private int gridViewScale = 32;
        public int GridViewScale { get => gridViewScale; set => SetProperty(ref gridViewScale, value); }

        public Dictionary<Idstring, PackageHeader> PackageHeaders;

        Dictionary<uint, FileEntry> FileEntries { get; set; }

        public ObservableCollection<EntryViewModel> ToRender { get; set; }
        public ObservableCollection<TreeEntryViewModel> FoldersToRender { get; set; }
        private List<Idstring> bundlesToRender;

        public List<Idstring> BundlesToRender { get => bundlesToRender; set => SetProperty(ref bundlesToRender, value); }

        public LinkedList<PageData> Pages = new LinkedList<PageData>();
        private LinkedListNode<PageData> CurrentPage;
        public string CurrentDir { get => CurrentPage.Value.Path; set => Navigate(value); }

        public DelegateCommand OpenFileDialog { get; }
        public DelegateCommand OpenAboutDialog { get; }
        public DelegateCommand OpenSettingsDialog { get; }
        public DelegateCommand OpenFindDialog { get; }
        public DelegateCommand OpenBundleSelectorDialog { get; }
        public DelegateCommand ForwardDir { get; }
        public DelegateCommand BackDir { get; }
        public DelegateCommand OnKeyDown { get; }
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
            OpenBundleSelectorDialog = new DelegateCommand(OpenBundleSelectorDialogExec, () => Root != null);
            OpenFindDialog = new DelegateCommand(OpenFindDialogExec, () => Root != null);
            BackDir = new DelegateCommand(BackDirExec, ()=> CurrentPage.Previous != null);
            ForwardDir = new DelegateCommand(ForwardDirExec, ()=> CurrentPage.Next != null);
            OnKeyDown = new DelegateCommand(OnKeyDownExec);
            SetViewStyle = new DelegateCommand<string>(style => SetViewStyleExec(style, true));

            CurrentDir = "";
            PackageHeaders = new Dictionary<Idstring, PackageHeader>();
            BundlesToRender = new List<Idstring>();
            ToRender = new ObservableCollection<EntryViewModel>();
            FoldersToRender = new ObservableCollection<TreeEntryViewModel>();

            //ToRender.Add(new EntryViewModel(this, new FileEntry { Name = "test" }));

            FileManager = new TempFileManager();

            Status = "Start by opening a blb file. Press 'File->Open' and navigate to the assets directory of the game";

            Utils.OnMouseMoved += OnMouseMoved;
            OpenAboutDialog = new DelegateCommand(() => Utils.ShowDialog("AboutDialog"));
            OpenSettingsDialog = new DelegateCommand(() => Utils.ShowDialog("SettingsDialog", r => SetTheme()));

            SetTheme();
        }

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

        void SetTheme()
        {
            ResourceLocator.SetColorScheme(App.Current.Resources, 
                Settings.Data.DarkMode ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);
        }

        void OpenFindDialogExec()
        {
            Utils.ShowDialog("FindDialog", r => {
                string search = r.Parameters.GetValue<string>("Search");
                if (!string.IsNullOrEmpty(search))
                {
                    bool useRegex = FindDialogViewModel.UseRegex;
                    bool matchWord = FindDialogViewModel.MatchWord;
                    Navigate(new PageData($"Search Results: '{search}' (Use Regex: {useRegex}, Match Word: {matchWord})")
                    {
                        Search = search,
                        UseRegex = useRegex,
                        MatchWord = matchWord
                    });
                }
            });
        }

        void OpenBundleSelectorDialogExec()
        {
            Utils.ShowDialog("BundleSelectorDialog", r =>
            {

            });
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
                    DragDropController controller = new DragDropController(false);
                    foreach(EntryViewModel vm in ToRender)
                    {
                        if (vm.IsSelected)
                            controller.DoDragDrop(vm.Owner);
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
                Root = new TreeEntryViewModel(this, new FolderEntry { EntryPath = "", Name = "assets" });
                OpenFindDialog.RaiseCanExecuteChanged();
                OpenBundleSelectorDialog.RaiseCanExecuteChanged();

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
                    {
                        if (FileEntries.ContainsKey(be.ID))
                            FileEntries[be.ID].AddBundleEntry(be);
                    }

                    PackageHeaders.Add(bundle.Name, bundle);
                }
            });

            GC.Collect();

            Status = "Done";

            RenderNewItems();

            Pages.Clear();
            CurrentDir = "";

            BundlesToRender = PackageHeaders.Keys.ToList();
            FoldersToRender.Clear();
            FoldersToRender.Add(Root);
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

            string search = CurrentPage.Value.Search;
            if (!string.IsNullOrEmpty(search))
            {
                children = Root.Owner.GetEntriesByConiditions(entry =>
                {
                    if (CurrentPage.Value.UseRegex)
                        return Regex.IsMatch(entry.Name, search);
                    else if (CurrentPage.Value.MatchWord)
                        return entry.Name == search;
                    else
                        return entry.Name.Contains(search);
                });
            } else
                children = Root.Owner.GetEntriesByDirectory(CurrentDir);

            bool disEmpty = Settings.Data.DisplayEmptyFiles;
            foreach (var entry in children)
            {
                if(!(entry is FileEntry) || disEmpty || ((entry as FileEntry).Size > 0))
                    ToRender.Add(new EntryViewModel(this, entry));
            }
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
