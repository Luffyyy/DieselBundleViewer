using DieselBundleViewer.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DieselBundleViewer.ViewModels
{
    public class SettingsDialogViewModel : BindableBase, IDialogAware
    {
        private bool extractFullDir;
        public bool ExtractFullDir { get => extractFullDir; set => SetProperty(ref extractFullDir, value); }

        private bool displayEmptyFiles;
        public bool DisplayEmptyFiles { get => displayEmptyFiles; set => SetProperty(ref displayEmptyFiles, value); }


        public event Action<IDialogResult> RequestClose;

        public string Title => "Settings";

        public DelegateCommand<string> CloseDialog { get; }

        public SettingsDialogViewModel()
        {
            CloseDialog = new DelegateCommand<string>(CloseDialogExec);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            DisplayEmptyFiles = Settings.Data.DisplayEmptyFiles;
            ExtractFullDir = Settings.Data.ExtractFullDir;
        }

        public bool CanCloseDialog () => true;

        private void CloseDialogExec(string success)
        {
            bool succ = success == "True";
            if (succ)
            {
                Settings.Data.DisplayEmptyFiles = DisplayEmptyFiles;
                Settings.Data.ExtractFullDir = ExtractFullDir;
                Settings.SaveSettings();
            }
            RequestClose?.Invoke(new DialogResult(success == "True" ? ButtonResult.OK : ButtonResult.Cancel));
        }

        public void OnDialogClosed()
        {
            
        }
    }
}
