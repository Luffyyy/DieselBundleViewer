using System;
using System.Collections.Generic;
using System.Text;

namespace DieselBundleViewer.Objects
{
    public enum Sorting
    {
        Name,
        Type,
        Size
    }

    public class PageData(string path)
    {
        public string Path { get; set; } = path;
        public string Search { get; set; }
        public bool MatchWord { get; set; }
        public bool UseRegex { get; set; }
        public bool IsSearch { get; set; }
        public bool FullPath { get; set; }
        public Sorting SortBy { get; set; } = Sorting.Name;
        public bool Ascending { get; set; } = true;
    }
}
