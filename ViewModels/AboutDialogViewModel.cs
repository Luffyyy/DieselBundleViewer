using DieselBundleViewer.Services;

namespace DieselBundleViewer.ViewModels
{
    public partial class AboutDialogViewModel : DialogBase
    {
        public override string Title => "About";
        public string Version => $"Version {Utils.Version}";
    }
}
