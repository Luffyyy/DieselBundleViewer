using DieselBundleViewer.Services;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace DieselBundleViewer.ViewModels
{
    public class DialogBase : BindableBase, IDialogAware
    {
        public virtual string Title => "";

        public DelegateCommand<string> CloseDialog { get; }

        protected IDialogParameters Params;

        public bool IsClosed { get; private set; }

        public DialogCloseListener RequestClose { get; }

        public DialogBase()
        {
            CloseDialog = new DelegateCommand<string>(CloseDialogExec);
            Utils.DialogsOpen.Add(this);
        }

        public void OnDialogOpened(IDialogParameters pms)
        {
            Params = pms;
            PostDialogOpened(pms);
        }

        protected virtual void PostDialogOpened(IDialogParameters pms) {

        }

        public virtual bool CanCloseDialog() => true;

        public virtual void OnDialogClosed() {
            IsClosed = true;
            Utils.DialogsOpen.Remove(this);
        }

        private void CloseDialogExec(string success)
        {
            PreCloseDialog(success);
            RequestClose.Invoke(Params, success == "True" ? ButtonResult.OK : ButtonResult.Cancel);
        }

        protected virtual void PreCloseDialog(string success) { }
    }
}
