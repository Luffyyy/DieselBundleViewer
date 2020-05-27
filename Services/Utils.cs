using DieselBundleViewer.ViewModels;
using DieselBundleViewer.Views;
using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// The last mouse position
        /// </summary>
        public static Point MousePos
        {
            get => mousePos;
            set
            {
                mousePos = value;
                OnMouseMoved?.Invoke(value);
            }
        }

        /// <summary>
        /// Opens a dialog without parameters.
        /// </summary>
        /// <param name="name">Name of the type of the dialog</param>
        /// <param name="res">An action that will execute when the dialog closes </param>
        /// <param name="modal">Whether or not the dialog is a modal dialog. Basically it doesn't let the user minimize it and such.</param>
        public static void ShowDialog(string name, Action<IDialogResult> res = null, bool modal = false)
        {
            ShowDialog(name, new DialogParameters(), res ?? (r => { }), modal);
        }

        /// <summary>
        /// Opens a dialog with parameters.
        /// </summary>
        /// <param name="name">Name of the type of the dialog</param>
        /// <param name="pms">Parameters to give to the dialog</param>
        /// <param name="res">An action that will execute when the dialog closes </param>
        /// <param name="modal">Whether or not the dialog is a modal dialog. Basically it doesn't let the user minimize it and such.</param>
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

        /// <summary>
        /// Returns whether or not an instance of a dialog is opened
        /// </summary>
        /// <param name="name">The name of the dialog type to check. For example: AboutDialog</param>
        public static bool DialogOpened(string name)
        {
            foreach (var dialog in DialogsOpen)
            {
                if (dialog.GetType().Name == (name + "ViewModel"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the directory of the path. Added because Path.GetFileName is only for files and the directory equivalent sucks.
        /// </summary>
        /// <param name="path">The path you want to get the directory of</param>
        /// <returns>Directory of a path</returns>
        public static string GetDirectory(string path)
        {
            string[] splt = path.Split("/");
            return string.Join("/", splt.Take(splt.Count() - 1));
        }

        /// <summary>
        /// Like Directory.Combine but not focused on windows (doesn't use \ at all) so we can combine ingame directories in peace.
        /// </summary>
        /// <param name="dir">First part of the directory to combine</param>
        /// <param name="dir2">Second part of the directory to combine</param>
        /// <returns>Combined directory</returns>
        public static string CombineDir(string dir, string dir2)
        {
            if (string.IsNullOrWhiteSpace(dir))
                return dir2;
            else
                return dir + "/" + dir2;
        }

        /// <summary>
        /// Returns a friendly file size from a ulong size. Example 1024 -> 1KiB.
        /// </summary>
        /// <param name="size">The size you wish to convert</param>
        /// <returns>The string of the friendly size</returns>
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
                    if (size < gb)
                        return string.Format("{0:n1}", (float)size / mb) + " MiB";
                    else
                        return string.Format("{0:n1}", (float)size / gb) + " GiB";
                }
            }
        }

        /// <summary>
        /// Opens a URL in the browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        public static void OpenURL(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public static bool IsRaid()
        {
            if (CurrentWindow != null && !string.IsNullOrEmpty(CurrentWindow.AssetsDir))
                return CurrentWindow.AssetsDir.Contains("RAID World War II");
            else
                return false;
        }
}
}
