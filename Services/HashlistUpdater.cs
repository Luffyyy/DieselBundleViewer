using DieselEngineFormats.Bundle;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DieselBundleViewer.Services
{
    public static class HashlistUpdater
    {
        private const string _localHashlistPath = "Data/hashlist";
        private const string _remoteHashlistInfoUrl = "https://api.github.com/repos/Luffyyy/PAYDAY-2-Hashlist/branches/master";
        private const string _remoteHashlistLatestUrl = "https://raw.githubusercontent.com/Luffyyy/PAYDAY-2-Hashlist/master/hashlist";
        private const string _firefoxUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0";

        private static DateTime? _localHashlistLatestUpdate = null;
        private static DateTime? _remoteHashlistLatestUpdate = null;
        private static HttpClient _httpClient = null;

        public static async Task DownloadLatestHashlist()
        {
            var client = GetOrInitHttpClient();
            var response = await client.GetStringAsync(_remoteHashlistLatestUrl);

            using var streamWriter = new StreamWriter(_localHashlistPath, false);
            streamWriter.Write(response.ToString());
            streamWriter.Flush();
            streamWriter.Close();

            ReloadHashIndex();
        }

        public static bool? IsHashlistUpToDate()
        {
            if (!File.Exists(_localHashlistPath))
                return null;

            if (!_localHashlistLatestUpdate.HasValue)
                _localHashlistLatestUpdate = File.GetLastWriteTimeUtc(_localHashlistPath);

            if (!_remoteHashlistLatestUpdate.HasValue)
            {
                var client = GetOrInitHttpClient();

                var response = client.GetStringAsync(_remoteHashlistInfoUrl);
                var branchInfo = JsonNode.Parse(response.Result.ToString());

                // The path down the JSON here is a bit annoying but I don't need any of this data
                var latestDate = branchInfo?["commit"]["commit"]["author"]["date"]?.ToString();

                if (latestDate != null)
                    _remoteHashlistLatestUpdate = DateTime.Parse(latestDate);
            }

            return _localHashlistLatestUpdate >= _remoteHashlistLatestUpdate;
        }

        private static HttpClient GetOrInitHttpClient()
        {
            if (_httpClient == null)
            {
                HttpClient client = new HttpClient();
                // GitHub API has a free limit but not having a User-Agent excludes you from even that
                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    _firefoxUserAgent
                );

                _httpClient = client;
            }

            return _httpClient;
        }
        private static void ReloadHashIndex()
        {
            HashIndex.Clear();
            HashIndex.LoadParallel(_localHashlistPath);
        }
    }
}
