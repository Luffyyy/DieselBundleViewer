using DieselBundleViewer.ViewModels;
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

        private void Click_DownloadButton(object sender, System.Windows.RoutedEventArgs e)
        {
            (DataContext as UpdateHashlistDialogViewModel).Click_DownloadHashlist();
        }
    }
}