using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DieselBundleViewer.ViewModels
{
    public class BundleSelectorDialogViewModel : DialogBase
    {
        public override string Title => "Select Bundles";
    }
}
