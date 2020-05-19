using DieselBundleViewer.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DieselBundleViewer.ViewModels
{
    public class SettingsDialogViewModel : DialogBase
    {
        private bool extractFullDir;
        public bool ExtractFullDir { get => extractFullDir; set => SetProperty(ref extractFullDir, value); }

        private bool displayEmptyFiles;
        public bool DisplayEmptyFiles { get => displayEmptyFiles; set => SetProperty(ref displayEmptyFiles, value); }

        private bool darkMode;
        public bool DarkMode { get => darkMode; set => SetProperty(ref darkMode, value); }

        public override string Title => "Settings";

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            DisplayEmptyFiles = Settings.Data.DisplayEmptyFiles;
            ExtractFullDir = Settings.Data.ExtractFullDir;
            DarkMode = Settings.Data.DarkMode;
        }

        protected override void PreCloseDialog(string success)
        {
            bool succ = success == "True";
            if (succ)
            {
                Settings.Data.DisplayEmptyFiles = DisplayEmptyFiles;
                Settings.Data.ExtractFullDir = ExtractFullDir;
                Settings.Data.DarkMode = DarkMode;
                Settings.SaveSettings();
            }
        }
    }
}
