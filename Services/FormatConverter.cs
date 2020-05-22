using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DieselBundleViewer.Services
{
    public class FormatConverter
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Extension { get; set; }
        public bool RequiresAttention { get; set; } = true;
        public string Type { get; set; }

        public delegate object ExportEventDel(MemoryStream ms, bool arg0);
        public ExportEventDel ExportEvent { get; set; }

        public object Export(MemoryStream ms, bool arg0 = false)
        {
            if (ExportEvent != null)
                return ExportEvent(ms, arg0);
            return ms;
        }

        //TEMPORARY
        public delegate void SaveEventDel(Stream ms, string toPath);
        public SaveEventDel SaveEvent { get; set; }

        public delegate object ImportEventDel(string path);
        public ImportEventDel ImportEvent { get; set; }

        public object Import(string path)
        {
            if (ImportEvent != null)
                return ImportEvent(path);
            return null;
        }
    }
}
