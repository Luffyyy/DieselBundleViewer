using DieselBundleViewer.Services;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace DieselBundleViewer.Views
{
    /// <summary>
    /// Interaction logic for AboutDialog
    /// </summary>
    public partial class AboutDialog : UserControl
    {
        public AboutDialog()
        {
            InitializeComponent();
        }

        private void Hyperlink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.OpenURL((sender as Hyperlink).NavigateUri.AbsoluteUri);
        }
    }
}
