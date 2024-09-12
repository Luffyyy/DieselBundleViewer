using DieselBundleViewer.Services;
using Prism.Dialogs;
using System;

namespace DieselBundleViewer.ViewModels
{
    public partial class UpdateHashlistDialogViewModel : DialogBase
    {
        public override string Title => "Hashlist Updater";

        private DateTime? _localHashlistLMD = null;
        private DateTime? _latestHashlistLMD = null;

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

            if (hashlistStatus == null)
            {
                UpdateWindowText = "No hashlist found locally.";
                DownloadButtonEnabled = true;
            }
            else if (hashlistStatus == true)
            {
                UpdateWindowText = "Hashlist is up to date.";
            }
            else
            {
                UpdateWindowText = "Hashlist update found. Please download.";
                DownloadButtonEnabled = true;
            }
        }
    }
}
