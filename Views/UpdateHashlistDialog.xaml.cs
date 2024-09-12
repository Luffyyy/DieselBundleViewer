using DieselBundleViewer.Services;
using System.Windows.Controls;

namespace DieselBundleViewer.Views
{
    /// <summary>
    /// Interaction logic for UpdateHashlistDialog
    /// </summary>
    public partial class UpdateHashlistDialog : UserControl
    {
        public UpdateHashlistDialog()
        {
            InitializeComponent();
        }

        private async void Click_DownloadButton(object sender, System.Windows.RoutedEventArgs e)
        {
            await HashlistUpdater.DownloadLatestHashlist();
        }
    }
}