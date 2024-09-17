using DieselBundleViewer.Services;
using Prism.Dialogs;
using System;
using System.Windows;

namespace DieselBundleViewer.ViewModels
{
    public partial class UpdateHashlistDialogViewModel : DialogBase
    {
        public override string Title => "Hashlist Updater";

        private string _hashlistText = "";
        public string UpdateWindowText
        {
            get => _hashlistText;
            set => SetProperty(ref _hashlistText, value);
        }

        private bool _downloadButtonEnabled = false;
        public bool DownloadButtonEnabled
        {
            get => _downloadButtonEnabled;
            set => SetProperty(ref _downloadButtonEnabled, value);
        }

        protected override void PostDialogOpened(IDialogParameters pms)
        {
            base.PostDialogOpened(pms);

            var hashlistStatus = HashlistUpdater.IsHashlistUpToDate();

            if (hashlistStatus == true)
            {
                UpdateWindowText = "Hashlist is up to date.";
                return;
            }
            else if (hashlistStatus == false)
            {
                UpdateWindowText = "Hashlist update found.";
            }
            else
            {
                UpdateWindowText = "No hashlist found locally.";
            }

            DownloadButtonEnabled = true;
        }

        public async void Click_DownloadHashlist()
        {
            UpdateWindowText = "Downloading... please wait...";
            DownloadButtonEnabled = false;

            await HashlistUpdater.DownloadLatestHashlist();

            UpdateWindowText = "Updated Hashlist.";

            // Clear the Tree view if it was opened without- or with an old hashlist
            var mainWindowViewModel = (MainWindowViewModel)Application.Current.MainWindow.DataContext;
            if (mainWindowViewModel != null && mainWindowViewModel.Bundles != null && mainWindowViewModel.Bundles.Count > 0)
                mainWindowViewModel.CloseBLB.Execute();
        }
    }
}
