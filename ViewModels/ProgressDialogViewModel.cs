using DieselBundleViewer.Services;
using DieselBundleViewer.Views;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DieselBundleViewer.ViewModels
{
    public class ProgressRecord
    {
        public string Status { get; private set; }
        public int Total { get; private set; }
        public int Completed { get; private set; }

        public ProgressRecord(string status, int total, int completed)
        {
            Status = status;
            Total = total;
            Completed = completed;
        }
    }

    public class ProgressDialogViewModel : DialogBase
    {
        private float progress;
        public float Progress { get => progress; set => SetProperty(ref progress, value); }
        private string status;
        public string Status { get => status; set => SetProperty(ref status, value); }

        private string progressText;
        public string ProgressText { get => progressText; set => SetProperty(ref progressText, value); }

        private CancellationTokenSource canceller;
        public CancellationTokenSource Canceller { get => canceller; set => SetProperty(ref canceller, value); }

        public DelegateCommand<string> CancelClicked { get;  }

        List<double> LastSecs { get; set; }
        Stopwatch TimerFinish { get; set; }

        public ProgressDialogViewModel()
        {
            CancelClicked = new DelegateCommand<string>(CancelClickedExec);
        }

        protected override void PostDialogOpened(IDialogParameters pms)
        {
            TimerFinish = new Stopwatch();
            LastSecs = new List<double>();

            TimerFinish.Start();
            Canceller = pms.GetValue<CancellationTokenSource>("Canceller");
            var startProgress = pms.GetValue<Action<ProgressDialogViewModel>>("ProgressAction");
            startProgress(this);
        }

        protected void CancelClickedExec(string s)
        {
            if(canceller != null)
            {
                canceller.Cancel();
            }
            else
            {
                CloseDialog.Execute(s);
            }
        }

        public void SetProgress(string status, int current, float total)
        {
            Status = status;
            Progress = Math.Clamp(100 * (current / total), 0, 100);

            LastSecs.Add(TimerFinish.Elapsed.TotalSeconds);

            TimerFinish.Restart();

            double estimate = 0;
            foreach (var secs in LastSecs)
            {
                estimate += secs;
            }
            estimate = (estimate / LastSecs.Count) * (total - current);

            TimeSpan span = TimeSpan.FromSeconds(estimate);
            string estimateTime = "";
            if (span.Hours > 0)
                estimateTime = string.Format("{0:D2}:{1:D2}:{2:D2}", span.Hours, span.Minutes, span.Seconds);
            else if (span.Minutes > 0)
                estimateTime = string.Format("{0:D2}:{1:D2}", span.Minutes, span.Seconds);
            else
                estimateTime = span.Seconds + " seconds";

            ProgressText = $"{current}/{total} ({Math.Round(progress, 2)}% ETA: {estimateTime})";

            if (progress == 100)
            {
                //We must call it using the UI thread so we can automatically close the dialog
                Application.Current.Dispatcher.Invoke(() => {
                    CloseDialog.Execute("True");
                });
                TimerFinish.Stop();
            }
        }

        public static void RunOperation(Func<IProgress<ProgressRecord>, CancellationToken, Task> operation)
        {
            var progress = new Progress<ProgressRecord>();
            var ct = new CancellationTokenSource();

            var pms = new DialogParameters();
            pms.Add("Canceller", ct);
            pms.Add("ProgressAction", new Action<ProgressDialogViewModel>(dialog =>
            {
                progress.ProgressChanged += (o, pr) =>
                {
                    dialog.SetProgress(pr.Status, pr.Completed, pr.Total);
                };
                var task = Task.Run(async () =>
                {
                    await Task.Delay(100);
                    try
                    {
                        await operation(progress, ct.Token);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(e.ToString(), "Operation failed");
                        });
                    }
                    Application.Current.Dispatcher.Invoke(() => {
                        dialog.CloseDialog.Execute("True");
                    });
                    ct.Dispose();
                });
            }));
            Utils.ShowDialog("ProgressDialog", pms);
        }
    }
}