using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DieselBundleViewer.ViewModels
{
    public class ProgressDialogViewModel : DialogBase
    {
        private float progress;
        public float Progress { get => progress; set => SetProperty(ref progress, value); }
        private string status;
        public string Status { get => status; set => SetProperty(ref status, value); }

        private string progressText;
        public string ProgressText { get => progressText; set => SetProperty(ref progressText, value); }

        protected override void PostDialogOpened(IDialogParameters pms)
        {
            var startProgress = pms.GetValue<Action<ProgressDialogViewModel>>("ProgressAction");
            startProgress(this);
        }

        public void SetProgress(string status, int current, float total)
        {
            Status = status;
            Progress = Math.Clamp(100 * (current / total), 0, 100);
            ProgressText = $"{current}/{total}";

            if (progress == 100)
            {
                //We must call it using the UI thread so we can automatically close the dialog
                Application.Current.Dispatcher.Invoke(() => {
                    CloseDialog.Execute("True");
                });
            }
        }
    }
}
