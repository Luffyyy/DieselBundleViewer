using DieselBundleViewer.Services;
using Prism.Dialogs;
using System.Collections.Generic;
using System.Linq;

namespace DieselBundleViewer.ViewModels
{
    public class ConvertFileDialogViewModel : DialogBase
    {
        public override string Title => "Choose a format for the file";

        private List<FormatConverter> formats;
        public List<FormatConverter> Formats { get => formats; set => SetProperty(ref formats, value); }

        private FormatConverter format;
        public FormatConverter Format { get => format; set => SetProperty(ref format, value); }

        protected override void PostDialogOpened(IDialogParameters parameters)
        {
            Formats = parameters.GetValue<List<FormatConverter>>("Formats");
            Format = formats.First();
        }

        protected override void PreCloseDialog(string success)
        {
            if (success == "True")
                Params.Add("Format", Format);
        }
    }
}
