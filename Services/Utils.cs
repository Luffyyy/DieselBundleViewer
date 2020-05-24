using DieselBundleViewer.ViewModels;
using DieselBundleViewer.Views;
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
    public static class Utils
    {
        public static string Version {
            get
            {
                Version ver = Assembly.GetEntryAssembly().GetName().Version;
                string hotfix = ver.Build != 0 ? ("." + ver.Build) : "";
                return $"{ver.Major}.{ver.Minor}{hotfix}";
            }
        }
        private static Point mousePos;
        public delegate void OnMouseMovedEvent(Point move);
        public static OnMouseMovedEvent OnMouseMoved;

        public static MainWindowViewModel CurrentWindow { get; set; }
        public static IDialogService CurrentDialogService { get; set; }

        public static List<DialogBase> DialogsOpen = new List<DialogBase>();

        public static Point MousePos
        {
            get => mousePos;
            set
            {
                mousePos = value;
                OnMouseMoved?.Invoke(value);
            }
        }

        public static void ShowDialog(string name, Action<IDialogResult> res = null, bool modal = false)
        {
            ShowDialog(name, new DialogParameters(), res ?? (r => { }), modal);
        }
        public static void ShowDialog(string name, DialogParameters pms, Action<IDialogResult> res = null, bool modal = false)
        {
            if (CurrentDialogService == null)
            {
                Console.WriteLine("Couldn't open dialog. Dialog service was not found.");
                return;
            }
            if (modal)
                CurrentDialogService.ShowDialog(name, pms, res ?? (r => { }));
            else
                CurrentDialogService.Show(name, pms, res ?? (r => { }));
        }

        public static bool DialogOpened(string name)
        {
            foreach(var dialog in DialogsOpen)
            {
                if (dialog.GetType().Name == (name + "ViewModel"))
                    return true;
            }
            return false;
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

        public static string FriendlySize(ulong size)
        {
            if (size < 1024)
                return size.ToString() + " B";
            else
            {
                double mb = Math.Pow(1024, 2);
                if (size < mb)
                    return string.Format("{0:n1}", (float)size / 1024) + " KiB";
                else
                {
                    double gb = Math.Pow(1024, 3);
                    if(size < gb)
                        return string.Format("{0:n1}", (float)size / mb) + " MiB";
                    else
                        return string.Format("{0:n1}", (float)size / gb) + " GiB";
                }
            }
        }
    }
}
