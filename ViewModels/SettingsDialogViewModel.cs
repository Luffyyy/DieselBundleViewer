using DieselBundleViewer.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DieselBundleViewer.ViewModels
{
    public class ExtReplacer : BindableBase
    {
        private string extension;
        public string Extension { get => extension; set => SetProperty(ref extension, value); }

        private string replaceWith;
        public string ReplaceWith { get => replaceWith; set => SetProperty(ref replaceWith, value); }
    }

    public class SettingsDialogViewModel : DialogBase
    {
        private bool extractFullDir;
        public bool ExtractFullDir { get => extractFullDir; set => SetProperty(ref extractFullDir, value); }

        private bool displayEmptyFiles;
        public bool DisplayEmptyFiles { get => displayEmptyFiles; set => SetProperty(ref displayEmptyFiles, value); }

        private bool darkMode;
        public bool DarkMode { get => darkMode; set => SetProperty(ref darkMode, value); }

        private bool showConsole;
        public bool ShowConsole { get => showConsole; set => SetProperty(ref showConsole, value); }

        private bool autoConvertFiles;
        public bool AutoConvertFiles { get => autoConvertFiles; set => SetProperty(ref autoConvertFiles, value); }

        private ExtReplacer selectedExtReplacer;
        public ExtReplacer SelectedExtReplacer { 
            get => selectedExtReplacer; 
            set {
                SetProperty(ref selectedExtReplacer, value);
                if (selectedExtReplacer != null)
                {
                    ExtensionTextBox = selectedExtReplacer.Extension;
                    ReplaceWithTextBox = selectedExtReplacer.ReplaceWith;
                }
            }
        }

        public ObservableCollection<ListItemViewModelBase> Types { get; set; }
        public ObservableCollection<ExtReplacer> ExtReplacers { get; set; }

        public DelegateCommand ApplyExtReplacer { get; set; }
        public DelegateCommand RemoveExtReplacer { get; set; }

        private string replaceWithTextBox;
        public string ReplaceWithTextBox { get => replaceWithTextBox; set => SetProperty(ref replaceWithTextBox, value); }

        private string extensionTextBox;
        public string ExtensionTextBox { get => extensionTextBox; set => SetProperty(ref extensionTextBox, value); }

        public override string Title => "Settings";
        public SettingsDialogViewModel() : base()
        {
            Types = new ObservableCollection<ListItemViewModelBase>();
            ExtReplacers = new ObservableCollection<ExtReplacer>();

            RemoveExtReplacer = new DelegateCommand(RemoveExtReplacerExec);
            ApplyExtReplacer = new DelegateCommand(ApplyExtReplacerExec);
        }

        private void ApplyExtReplacerExec()
        {
            if (SelectedExtReplacer == null)
                ExtReplacers.Add(new ExtReplacer { Extension = ExtensionTextBox, ReplaceWith = ReplaceWithTextBox });
            else
            {
                selectedExtReplacer.Extension = ExtensionTextBox;
                selectedExtReplacer.ReplaceWith = ReplaceWithTextBox;
                SelectedExtReplacer = null;
            }
        }
        private void RemoveExtReplacerExec()
        {
            ExtReplacers.Remove(selectedExtReplacer);
        }

        protected override void PostDialogOpened(IDialogParameters parameters)
        {
            DisplayEmptyFiles = Settings.Data.DisplayEmptyFiles;
            ExtractFullDir = Settings.Data.ExtractFullDir;
            DarkMode = Settings.Data.DarkMode;
            var exts = Definitions.Extensions;
            var selectedExts = Settings.Data.SelectedExtensions;
            Types.Clear();
            foreach (string ext in exts)
            {
                Types.Add(new ListItemViewModelBase { Name = ext, IsSelected = selectedExts.Contains(ext) });
            }

            var savedExtReplacers = Settings.Data.ExtReplacers;
            ExtReplacers.Clear();
            foreach (string[] replacer in savedExtReplacers)
            {
                ExtReplacers.Add(new ExtReplacer { Extension = replacer[0], ReplaceWith = replacer[1]});
            }
        }

        protected override void PreCloseDialog(string success)
        {
            bool succ = success == "True";
            if (succ)
            {
                if (DisplayEmptyFiles != Settings.Data.DisplayEmptyFiles && Utils.CurrentWindow.Root != null)
                    Utils.CurrentWindow.Root.Children = null;

                Settings.Data.DisplayEmptyFiles = DisplayEmptyFiles;
                Settings.Data.ExtractFullDir = ExtractFullDir;
                Settings.Data.DarkMode = DarkMode;
                Settings.Data.AutoConvertFiles = AutoConvertFiles;

                Settings.Data.ExtReplacers.Clear();
                foreach(var replacer in ExtReplacers)
                {
                    Settings.Data.ExtReplacers.Add(new string[]{ replacer.Extension, replacer.ReplaceWith });
                }
                List<string> selectedExtensions = new List<string>();
                foreach (var ext in Types)
                {
                    if (ext.IsSelected)
                        selectedExtensions.Add(ext.Name);
                }
                Settings.Data.SelectedExtensions = selectedExtensions;
                Settings.SaveSettings();
            }
        }
    }
}
