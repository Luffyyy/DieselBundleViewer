using DieselBundleViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DieselBundleViewer.Views
{
    /// <summary>
    /// Interaction logic for EntryGridView.xaml
    /// </summary>
    public partial class EntryGridView : UserControl
    {
        private FixListView fix;
        public EntryGridView()
        {
            fix = new FixListView();
            InitializeComponent();
        }

        private void ListPreviewKeydown(object sender, KeyEventArgs e)
        {
            fix.ListPreviewKeydown(sender, e);
        }

        private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fix.ListSelectionChanged(sender, e);
        }

        private void ListPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            fix.ListPreviewMouseDown(sender, e);
        }

        private void ListItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            fix.ListItemPreviewMouseLeftButtonDown(sender, e);
        }
    }
}
