using DieselBundleViewer.Services;
using Prism.Dialogs;

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

        private bool showConsole;
        public bool ShowConsole { get => showConsole; set => SetProperty(ref showConsole, value); }

        public override string Title => "Settings";

        protected override void PostDialogOpened(IDialogParameters parameters)
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
                if (DisplayEmptyFiles != Settings.Data.DisplayEmptyFiles && Utils.CurrentWindow.Root != null)
                    Utils.CurrentWindow.Root.Children = null;

                Settings.Data.DisplayEmptyFiles = DisplayEmptyFiles;
                Settings.Data.ExtractFullDir = ExtractFullDir;
                Settings.Data.DarkMode = DarkMode;
                Settings.SaveSettings();
            }
        }
    }
}
