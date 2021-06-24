using DieselBundleViewer.Services;
using DieselBundleViewer.ViewModels;
using DieselEngineFormats.Bundle;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace DieselBundleViewer.Models
{
    public class FolderEntry : IEntry
    {
        public SortedDictionary<string, IEntry> Children { get; set; }

        public FolderEntry Parent { get; set; }
        public string EntryPath { get; set; }
        public string Name { get; set; }
        public uint Size => 0;

        private ulong? totalSize;
        public ulong TotalSize { 
            get
            {
                if (totalSize != null)
                    return (ulong)totalSize;

                ulong size = 0;
                var children = GetAllChildren();
                foreach (var entry in children)
                {
                    if (entry is FileEntry)
                        size += entry.Size;
                }
                totalSize = size;
                return size;
            }
        }

        public string Type => "File folder";

        public string SaveName => Name;

        public string SavePath => EntryPath;

        private uint folderLevel;

        public FolderEntry(uint level = 0) {
            folderLevel = level;
            Children = new SortedDictionary<string, IEntry>();
        }

        public FolderEntry(FileEntry entry, uint level = 0) : this(level)
        {
            AddFileEntry(entry);
        }

        public FolderEntry(Dictionary<uint, FileEntry> ents, uint level = 0) : this(level)
        {
            foreach (KeyValuePair<uint, FileEntry> entry in ents)
            {
                AddFileEntry(entry.Value);
            }
        }

        public bool InBundle(Idstring name)
        {
            foreach(var child in Children)
            {
                if (child.Value.InBundle(name))
                    return true;
            }
            return false;
        }

        public bool InBundles(List<Idstring> names)
        {
            foreach (var child in Children)
            {
                if (child.Value.InBundles(names))
                    return true;
            }
            return false;
        }

        public bool HasVisibleFiles()
        {
            if (Settings.Data.DisplayEmptyFiles)
                return true;

            foreach (var child in Children.Values)
            {
                if ((child is FolderEntry folder && folder.HasVisibleFiles()) || child.Size > 0)
                    return true;
            }
            return false;
        }

        public List<object> ChildObjects(Idstring pck = null)
        {
            List<object> objs = new List<object>();
            var children = Children.Values;

            foreach (var child in children)
            {
                if (child is FolderEntry entry && (pck == null || entry.ContainsAnyBundleEntries(pck)))
                    objs.Add(child);
            }

            foreach (var child in children)
            {
                if ((!(child is FileEntry entry) || pck == null || entry.BundleEntries.ContainsKey(pck)) && !(child is FolderEntry))
                    objs.Add(child);
            }

            return objs;
        }

        public void AddFileEntry(FileEntry entry)
        {
            string[] splt = entry.PathIds.HasUnHashed ? entry.PathIds.UnHashed.Split("/") : null;
            if (splt != null && splt.Length > (folderLevel + 1))
            {
                string initial_folder = splt[folderLevel];
                if (!Children.ContainsKey(initial_folder))
                {
                    FolderEntry folder = new FolderEntry(entry, this.folderLevel + 1) { Parent = this, EntryPath = "" };
                    for (int i = 0; i <= this.folderLevel; i++)
                    {
                        folder.EntryPath = Utils.CombineDir(folder.EntryPath, splt[i]);
                    }
                    //Debug.Print(string.Format("Folder: {0}", folder.Path));

                    folder.Name = initial_folder;
                    Children.Add(initial_folder, folder);
                }
                else
                {
                    ((FolderEntry)Children[initial_folder]).AddFileEntry(entry);
                }
            }
            else
            {
                entry.Parent = this;
                Children.Add(entry.Name, entry);
            }
        }

        public void AddToTree(FolderEntry item, Idstring pck = null)
        {
            foreach (KeyValuePair<string, IEntry> entry in Children)
            {
                if (entry.Value is FolderEntry)
                {
                    FolderEntry _entry = entry.Value as FolderEntry;

                    if (pck != null && !_entry.ContainsAnyBundleEntries(pck))
                        continue;

                  //  item.Children.Add(item);
                    _entry.AddToTree(item, pck);
                }
            }
        }

        public bool ContainsAnyBundleEntries(Idstring package = null)
        {
            foreach (KeyValuePair<string, IEntry> entry in Children)
            {
                if (entry.Value is FolderEntry)
                {
                    FolderEntry _entry = entry.Value as FolderEntry;
                    if (_entry.ContainsAnyBundleEntries(package))
                    {
                        return true;
                    }
                }
                else if (entry.Value is FileEntry)
                {
                    FileEntry _entry = entry.Value as FileEntry;
                    if (_entry.BundleEntries.Count != 0 && (package != null ? _entry.BundleEntries.ContainsKey(package) : true))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<IEntry> GetAllChildren(bool ignoreSelectedBundles = false, List<IEntry> list = null)
        {
            if (list == null)
                list = new List<IEntry>();

            var selectedBundles = Utils.CurrentWindow.SelectedBundles;

            foreach (KeyValuePair<string, IEntry> pairEntry in Children)
            {
                IEntry entry = pairEntry.Value;
                if (ignoreSelectedBundles || selectedBundles.Count == 0 || entry.InBundles(selectedBundles))
                {
                    list.Add(entry);
                    if (entry is FolderEntry)
                        (entry as FolderEntry).GetAllChildren(ignoreSelectedBundles, list);
                }
            }

            return list;
        }

        public void ForEachEntryInDirectory(string dir, Action<IEntry> func = null)
        {
            foreach (KeyValuePair<string, IEntry> pairEntry in Children)
            {
                IEntry entry = pairEntry.Value;

                if (Utils.GetDirectory(entry.EntryPath) == dir)
                    func(entry);

                if (entry is FolderEntry && dir.StartsWith(entry.EntryPath))
                    (entry as FolderEntry).ForEachEntryInDirectory(dir, func);
            }
        }


        public void ForEachEntry(Action<IEntry> func)
        {
            foreach (KeyValuePair<string, IEntry> pairEntry in Children)
            {
                IEntry entry = pairEntry.Value;
                func(entry);
                if (entry is FolderEntry)
                    (entry as FolderEntry).ForEachEntry(func);
            }
        }

        public void ResetTotalSize()
        {
            totalSize = null;
        }
    }
}
