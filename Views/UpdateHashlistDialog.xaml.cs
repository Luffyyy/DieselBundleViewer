using System.ComponentModel;
using System.IO;
using System.Net.Http;
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void Update_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0"
            );

            var response = client.GetStringAsync("https://raw.githubusercontent.com/Luffyyy/PAYDAY-2-Hashlist/master/hashlist");

            using var streamWriter = new StreamWriter("data/hashlist", false);
            streamWriter.Write(response.Result.ToString());
            streamWriter.Flush();
            streamWriter.Close();

            this.UpdateNeededText.Text = "Update completed. Please restart.";
            this.UpdateButton.Content = "Restart";
            this.UpdateButton.Click += UpdateButton_PostUpdate_Click;
        }

        private void UpdateButton_PostUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Forms.Application.Restart();
            System.Windows.Application.Current.Shutdown();
        }
    }
}