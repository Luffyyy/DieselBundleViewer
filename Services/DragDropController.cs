using DieselBundleViewer.Models;
using DieselBundleViewer.ViewModels;
using DieselEngineFormats.Bundle;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DieselBundleViewer.Services
{    public class StartProgress : PubSubEvent { }

    class DragDropController
    {
        public bool OutputFullPaths = false;
        private TextBox Control { get; set; }

        private ProgressDialogViewModel Progress;

        private EventAggregator Aggregator;

        public DragDropController(bool outputFullPaths)
        {
            Control = new TextBox();
            OutputFullPaths = outputFullPaths;
        }

        public void DoDragDrop(List<IEntry> entries)
        {
            VirtualFileDataObject virtualFileDataObject = new VirtualFileDataObject(StartDragDrop, EndDragDrop);
            List<VirtualFileDataObject.FileDescriptor> files = new List<VirtualFileDataObject.FileDescriptor>();

            foreach(var entry in entries)
            {
                if (entry is FileEntry file)
                    PopulateFile(files, file, entry.Name);
                else if(entry is FolderEntry folder)
                    PopulateFiles(files, folder, entry.Name);
            }

            //Show dialog only when there are more than 5 items moved.
            Console.WriteLine("Beginning with {0} files", files.Count);
            virtualFileDataObject.SetData(files);
            
            if(files.Count > 10)
            {
                Console.WriteLine("Progress");
                Aggregator = new EventAggregator();
                Aggregator.GetEvent<StartProgress>().Subscribe(() =>
                {
                    var pms = new DialogParameters();
                    pms.Add("ProgressAction", new Action<ProgressDialogViewModel>(dialog => {
                        Progress = dialog;
                    }));

                    Utils.ShowDialog("ProgressDialog", pms);
                }, ThreadOption.UIThread);
            }

            try
            {
                VirtualFileDataObject.DoDragDrop(Control, virtualFileDataObject, DragDropEffects.Copy);
            }
            catch (COMException)
            {
                Console.WriteLine("Failed Drag-Drop.");
            }
            Console.WriteLine("Finished Drag-Drop operation setup");
        }

        public void StartDragDrop(VirtualFileDataObject data)
        {
            if(Aggregator != null)
            {
                Thread t = new Thread(() => Aggregator.GetEvent<StartProgress>().Publish());
                t.IsBackground = true;
                t.Start();
            }
        }
        public void EndDragDrop(VirtualFileDataObject data)
        {

        }

        int i = 0;

        public void PopulateFile(List<VirtualFileDataObject.FileDescriptor> files, FileEntry parent, string path = "")
        {
            if (parent.BundleEntries.Count == 0)
                return;

            string name = OutputFullPaths ? parent.EntryPath : path;
            files.Add(new VirtualFileDataObject.FileDescriptor()
            {
                Name = name,
                StreamContents = (stream) =>
                {
                    i++;
                    int total = files.Count;

                    PackageFileEntry maxBundleEntry = parent.MaxBundleEntry();

                    byte[] bytes = parent.FileBytes(maxBundleEntry);
                    if (bytes != null)
                        stream.Write(bytes, 0, bytes.Length);
                    else
                        Console.WriteLine("Failed to extract {0} from package: {1}", name, maxBundleEntry.PackageName.ToString());

                    if(Progress != null)
                    {
                        if (Progress.IsClosed)
                            throw new Exception(); //No clue how to really stop that other than exceptions lol.
                        else
                            Progress.SetProgress($"Copying {parent.EntryPath}", i, files.Count);
                    }
                }
            });
        }

        public void PopulateFiles(List<VirtualFileDataObject.FileDescriptor> files, FolderEntry parent, string path = "")
        {
            foreach (KeyValuePair<string, IEntry> entry in parent.Children)
            {
                if (entry.Value is FolderEntry folder)
                    PopulateFiles(files, folder, Path.Combine(path ?? "", entry.Key ?? ""));
                else if (entry.Value is FileEntry file)
                    PopulateFile(files, file, Path.Combine(path ?? "", entry.Key));
            }
        }
    }
}
