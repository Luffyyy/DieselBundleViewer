using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DieselBundleViewer.ViewModels
{
    //Don't really like having empty classes like this just for events but oh well.
    public class SetProgressEvent : PubSubEvent<Tuple<string, float>> {}

    public class ProgressDialogViewModel : DialogBase
    {
        private float progress;
        public float Progress { get => progress; set => SetProperty(ref progress, value); }
        private string status;
        public string Status { get => status; set => SetProperty(ref status, value); }

        private Thread taskThread;

        protected override void PostDialogOpened(IDialogParameters pms)
        {
            SetProgressEvent setProgress = pms.GetValue<SetProgressEvent>("SetProgress");
            setProgress.Subscribe(SetProgress, ThreadOption.UIThread);

            taskThread = pms.GetValue<Thread>("TaskThread");
            taskThread.Start(this);
        }

        public void SetProgress(Tuple<string, float> t)
        {
            Status = t.Item1;
            Progress = Math.Clamp(t.Item2, 0, 100);

            if(progress == 100)
                CloseDialog.Execute("True");
        }
    }
}
