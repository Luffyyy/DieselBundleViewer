using DieselBundleViewer.Models;
using DieselEngineFormats.Bundle;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace DieselBundleViewer.Services
{
    class DragDropController
    {
        public bool OutputFullPaths = false;
        private TextBox Control { get; set; }

        public DragDropController(bool outputFullPaths)
        {
            Control = new TextBox();
            this.OutputFullPaths = outputFullPaths;
        }

        public void DoDragDrop(IEntry entry)
        {
            VirtualFileDataObject virtualFileDataObject = new VirtualFileDataObject();
            List<VirtualFileDataObject.FileDescriptor> files = new List<VirtualFileDataObject.FileDescriptor>();

            if (entry is FileEntry)
                PopulateFile(files, (FileEntry)entry, entry.Name);
            else if(entry is FolderEntry)
                PopulateFiles(files, (FolderEntry)entry, entry.Name);

            virtualFileDataObject.SetData(files);
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

        public void PopulateFile(List<VirtualFileDataObject.FileDescriptor> files, FileEntry parent, string path = "")
        {
            if (parent.BundleEntries.Count == 0)
            {
                Console.WriteLine("No bundle entries");
                return;
            }

            string name = OutputFullPaths ? parent.EntryPath : path;

            files.Add(new VirtualFileDataObject.FileDescriptor()
            {
                Name = name,
                StreamContents = () =>
                {
                    MemoryStream stream = new MemoryStream();
                    PackageFileEntry maxBundleEntry = parent.MaxBundleEntry();
                    byte[] bytes = parent.FileBytes(maxBundleEntry);
                    if (bytes != null)
                        stream.Write(bytes, 0, bytes.Length);
                    else
                        Console.WriteLine("Failed to extract {0} from package: {1}", name, maxBundleEntry.PackageName.ToString());

                    return stream;
                }
            });
        }

        public void PopulateFiles(List<VirtualFileDataObject.FileDescriptor> files, FolderEntry parent, string path = "")
        {
            foreach (KeyValuePair<string, IEntry> entry in parent.Children)
            {
                if (entry.Value is FolderEntry)
                    this.PopulateFiles(files, (FolderEntry)entry.Value, Path.Combine(path ?? "", entry.Key ?? ""));
                else if (entry.Value is FileEntry)
                    this.PopulateFile(files, (FileEntry)entry.Value, Path.Combine(path ?? "", entry.Key));
            }
        }
    }
}
