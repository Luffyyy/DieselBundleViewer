using DieselBundleViewer.Views;
using Prism.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace DieselBundleViewer.ViewModels
{
    public partial class UpdateHashlistDialogViewModel : DialogBase
    {
        public override string Title => "Hashlist Updater";

        private DateTime? _localHashlistLMD = null;
        private DateTime? _latestHashlistLMD = null;

        private string _hashlistText = "";
        public string HashlistVersionDifference
        {
            get
            {
                return _hashlistText;
            }
            set
            {
                SetProperty(ref _hashlistText, value);
            }
        }

        private bool _updateButtonEnabled = false;
        public bool UpdateButtonEnabled
        {
            get
            {
                return _updateButtonEnabled;
            }
            set
            {
                SetProperty(ref _updateButtonEnabled, value);
            }
        }

        protected override void PostDialogOpened(IDialogParameters pms)
        {
            base.PostDialogOpened(pms);

            if (File.Exists("Data/hashlist"))
                _localHashlistLMD = File.GetLastWriteTime("Data/hashlist");

            using var client = new HttpClient();
            // GitHub API has a free limit but not having a User-Agent excludes you from even that
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0"
            );

            var response = client.GetStringAsync("https://api.github.com/repos/Luffyyy/PAYDAY-2-Hashlist/branches/master");
            var branchInfo = JsonNode.Parse(response.Result.ToString());

            var latestDate = branchInfo?["commit"]["commit"]["author"]["date"]?.ToString();
            if (latestDate != null)
                _latestHashlistLMD = DateTime.Parse(latestDate);

            if (!_localHashlistLMD.HasValue)
                HashlistVersionDifference = "No hashlist found locally.";
            else if (_latestHashlistLMD.HasValue && _localHashlistLMD >= _latestHashlistLMD)
                HashlistVersionDifference = "Hashlist is up to date.";
            else
            {
                HashlistVersionDifference = "Hashlist update found.";
                UpdateButtonEnabled = true;
            }
        }
    }
}
