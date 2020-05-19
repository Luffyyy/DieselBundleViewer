using System;
using System.Collections.Generic;
using System.Text;

namespace DieselBundleViewer.Objects
{
    public class PageData
    {
        public string Path { get; set; }
        public string Search { get; set; }
        public bool MatchWord { get; set; }
        public bool UseRegex { get; set; }

        public PageData(string path)
        {
            Path = path;
        }
    }
}
