using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

namespace DieselBundleViewer.Services
{
    static class Utils
    {
        public static string Version {
            get
            {
                Version ver = Assembly.GetEntryAssembly().GetName().Version;
                string hotfix = ver.Build != 0 ? ("."+ver.Build) : "";
                return $"{ver.Major}.{ver.Minor}{hotfix}";
            }
        }
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

        public static void ShowDialog(string name, Action<IDialogResult> res=null)
        {
            ShowDialog(name, new DialogParameters(), res ?? (r => { }));
        }
        public static void ShowDialog(string name, DialogParameters pms, Action<IDialogResult> res)
        {
            if(CurrentDialogService == null)
            {
                Console.WriteLine("Couldn't open dialog. Dialog service was not found.");
                return;
            }
            CurrentDialogService.Show(name, pms, res);
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
