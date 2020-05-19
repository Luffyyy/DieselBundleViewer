using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;

namespace DieselBundleViewer.ViewModels
{
    public class DialogBase : BindableBase, IDialogAware
    {
        public virtual string Title => "";

        public event Action<IDialogResult> RequestClose;
        public DelegateCommand<string> CloseDialog { get; }

        protected IDialogParameters Params;

        public DialogBase()
        {
            CloseDialog = new DelegateCommand<string>(CloseDialogExec);
        }

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            Params = parameters;
        }

        public virtual bool CanCloseDialog() => true;

        public virtual void OnDialogClosed() { }

        private void CloseDialogExec(string success)
        {
            PreCloseDialog(success);
            RequestClose?.Invoke(new DialogResult(success == "True" ? ButtonResult.OK : ButtonResult.Cancel, Params));
        }

        protected virtual void PreCloseDialog(string success) { }
    }
}
