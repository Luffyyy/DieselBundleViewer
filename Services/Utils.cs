using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace DieselBundleViewer.Services
{
    static class Utils
    {
        public static string Version => "0.1";
        private static Point mousePos;
        public delegate void OnMouseMovedEvent(Point move);
        public static OnMouseMovedEvent OnMouseMoved;

        public static IDialogService CurrentDialogService { get; set; }

        public static Point MousePos
        {
            get => mousePos;
            set
            {
                mousePos = value;
                OnMouseMoved?.Invoke(value);
            }
        }

        public static string GetDirectory(string path)
        {
            string[] splt = path.Split("/");
            return string.Join("/", splt.Take(splt.Count() - 1));
        }

        public static string CombineDir(string dir, string dir2)
        {
            if (string.IsNullOrWhiteSpace(dir))
                return dir2;
            else
                return dir + "/" + dir2;
        }

    }
}
