using DieselBundleViewer.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DieselBundleViewer.ViewModels
{
    public partial class AboutDialogViewModel : DialogBase
    {
        public override string Title => "About";
        public string Version => $"Version {Utils.Version}";
    }
}
