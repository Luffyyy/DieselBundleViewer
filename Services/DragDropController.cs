﻿using DieselBundleViewer.Models;
using DieselBundleViewer.ViewModels;
using DieselEngineFormats.Bundle;
using Prism.Events;
using Prism.Dialogs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace DieselBundleViewer.Services
{    public class StartProgress : PubSubEvent { }

    class DragDropController(bool outputFullPaths)
    {
        public bool OutputFullPaths = outputFullPaths;
        private TextBox Control { get; set; } = new TextBox();

        private ProgressDialogViewModel Progress;

        private EventAggregator Aggregator;

        public void DoDragDrop(List<IEntry> entries)
        {
            VirtualFileDataObject virtualFileDataObject = new VirtualFileDataObject(StartDragDrop, EndDragDrop);
            var files = new List<VirtualFileDataObject.FileDescriptor>();

            string currentDir = Utils.CurrentWindow.CurrentDir;
            foreach (var entry in entries)
            {
                if (entry is FileEntry file)
                    PopulateFile(files, file, currentDir);
                else if(entry is FolderEntry folder)
                    PopulateFiles(files, folder, currentDir);
            }

            //Show dialog only when there are more than 5 items moved.
            Console.WriteLine("Beginning with {0} files", files.Count);
            virtualFileDataObject.SetData(files);
            
            if(files.Count > 10)
            {
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

        public void PopulateFile(List<VirtualFileDataObject.FileDescriptor> files, FileEntry parent, string removeDirectory)
        {
            if (parent.BundleEntries.Count == 0)
                return;

            string name = parent.EntryPath;
            if (!OutputFullPaths && !string.IsNullOrEmpty(removeDirectory))
                name = name.Replace(removeDirectory, "");

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

        public void PopulateFiles(List<VirtualFileDataObject.FileDescriptor> files, FolderEntry parent, string removeDirectory)
        {
            foreach (var entry in parent.GetAllChildren())
            {
                if (entry is FileEntry file)
                    PopulateFile(files, file, removeDirectory);
            }
        }
    }
}
