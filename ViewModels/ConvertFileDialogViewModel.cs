using DieselBundleViewer.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DieselBundleViewer.ViewModels
{
    public class ConvertFileDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "Choose a format for the file";
        public event Action<IDialogResult> RequestClose;

        private List<FormatConverter> formats;
        public List<FormatConverter> Formats { get => formats; set => SetProperty(ref formats, value); }

        private FormatConverter format;
        public FormatConverter Format { get => format; set => SetProperty(ref format, value); }

        IDialogParameters Params;

        public DelegateCommand<string> CloseDialog { get; }

        public ConvertFileDialogViewModel()
        {
            CloseDialog = new DelegateCommand<string>(CloseDialogExec);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Params = parameters;
            Formats = parameters.GetValue<List<FormatConverter>>("formats");
            Format = formats.First();
        }

        private void CloseDialogExec(string success)
        {
            bool succ = success == "True";
            if (succ)
                Params.Add("format", Format);
            RequestClose?.Invoke(new DialogResult(success == "True" ? ButtonResult.OK : ButtonResult.Cancel));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }
    }
}
