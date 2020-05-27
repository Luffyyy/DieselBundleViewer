using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DieselBundleViewer.Views
{
    /// <summary>
    /// Interaction logic for ConverFileDialog.xaml
    /// </summary>
    public partial class ConvertFileDialog : UserControl
    {
        public ConvertFileDialog()
        {
            InitializeComponent();
        }

        private void Button_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus((Button)sender);
        }
    }
}
