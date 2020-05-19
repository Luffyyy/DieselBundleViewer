using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DieselBundleViewer.Views
{
    /// <summary>
    /// Interaction logic for FindDialog
    /// </summary>
    public partial class FindDialog : UserControl
    {
        public FindDialog()
        {
            InitializeComponent();
        }

        private void Search_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus((TextBox)sender);
        }
    }
}
