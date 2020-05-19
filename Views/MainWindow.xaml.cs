using AdonisUI.Controls;
using DieselBundleViewer.Services;
using DieselBundleViewer.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DieselBundleViewer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Utils.MousePos = e.GetPosition(this);
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (DataContext as MainWindowViewModel).OnRelease();
        }

        private void ContentPresenter_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            (DataContext as MainWindowViewModel).OnMouseWheel(e);
        }
    }
}
