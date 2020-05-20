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
    public class FolderEntry : IChild, IParent, IEntry
    {
        public SortedDictionary<string, IEntry> Children { get; set; }

        public IParent Parent { get; set; }
        public string EntryPath { get; set; }
        public string Name { get; set; }
        public uint Size => 0;
        public MainWindowViewModel DataContext { get; set; }

        public string Type => "File folder";

        public List<object> ChildObjects(Idstring pck = null)
        {
            List<object> objs = new List<object>();
            foreach (KeyValuePair<string, IEntry> kvp in Children)
            {
                if (kvp.Value is IParent && (pck == null || ((IParent)kvp.Value).ContainsAnyBundleEntries(pck)))
                    objs.Add(kvp.Value);
            }

            foreach (KeyValuePair<string, IEntry> kvp in Children)
            {
                if (kvp.Value is IChild && (!(kvp.Value is FileEntry) || pck == null || ((FileEntry)kvp.Value).BundleEntries.FindIndex(item => item.Parent.Name.Equals(pck)) != -1) && !(kvp.Value is IParent))
                    objs.Add(kvp.Value);
            }

            return objs;
        }

        private uint folderLevel;

        public FolderEntry(uint level = 0) {
            folderLevel = level;
            Children = new SortedDictionary<string, IEntry>();
        }

        public FolderEntry(FileEntry entry, uint level = 0)
        {
            folderLevel = level;
            Children = new SortedDictionary<string, IEntry>();
            AddFileEntry(entry);
        }

        public FolderEntry(Dictionary<uint, FileEntry> ents, uint level = 0)
        {
            folderLevel = level;
            Children = new SortedDictionary<string, IEntry>();

            foreach (KeyValuePair<uint, FileEntry> entry in ents)
            {
                AddFileEntry(entry.Value);
            }
        }

        public void AddFileEntry(FileEntry entry)
        {
            int[] path_parts = entry._path.UnHashedParts;
            if (path_parts != null && path_parts.Length > (folderLevel + 1))
            {
                string initial_folder = HashIndex.LookupString(path_parts[folderLevel]);
                if (!Children.ContainsKey(initial_folder))
                {
                    FolderEntry folder = new FolderEntry(entry, this.folderLevel + 1) { Parent = this, EntryPath = "" };
                    for (int i = 0; i <= this.folderLevel; i++)
                    {
                        folder.EntryPath = Utils.CombineDir(folder.EntryPath, HashIndex.LookupString(path_parts[i]));
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
                if (entry.Value is IParent)
                {
                    IParent _entry = entry.Value as IParent;

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
                if (entry.Value is IParent)
                {
                    IParent _entry = entry.Value as IParent;
                    if (_entry.ContainsAnyBundleEntries(package))
                    {
                        return true;
                    }
                }
                else if (entry.Value is FileEntry)
                {
                    FileEntry _entry = entry.Value as FileEntry;
                    if (_entry.BundleEntries.Count != 0 && (package != null ? _entry.BundleEntries.FindIndex(ent => ent.Parent.Name.Equals(package)) != -1 : true))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void GetSubFileEntries(Dictionary<string, FileEntry> fileList)
        {
            foreach (KeyValuePair<string, IEntry> pairEntry in Children)
            {
                if (pairEntry.Value is FileEntry)
                {
                    FileEntry _entry = pairEntry.Value as FileEntry;
                    if (_entry.BundleEntries.Count == 0)
                        continue;

                    fileList.Add(_entry.EntryPath, _entry);

                }
                else if (pairEntry.Value is IParent)
                {
                    IParent _entry = pairEntry.Value as IParent;
                    _entry.GetSubFileEntries(fileList);
                }
            }
        }

        public List<IEntry> GetEntriesByDirectory(string dir, List<IEntry> entries=null)
        {
            if(entries == null)
                entries = new List<IEntry>();

            foreach (KeyValuePair<string, IEntry> pairEntry in Children)
            {
                IEntry entry = pairEntry.Value;

                if (Utils.GetDirectory(entry.EntryPath) == dir)
                {
                    if (pairEntry.Value is FolderEntry)
                        entries.Insert(0, entry);
                    else
                        entries.Add(entry);
                }
                if (entry is FolderEntry && dir.StartsWith(entry.EntryPath))
                    (entry as FolderEntry).GetEntriesByDirectory(dir, entries);

            }
            return entries;
        }


        public List<IEntry> GetEntriesByConiditions(Func<IEntry, bool> check, List<IEntry> entries = null)
        {
            if (entries == null)
                entries = new List<IEntry>();

            foreach (KeyValuePair<string, IEntry> pairEntry in Children)
            {
                IEntry entry = pairEntry.Value;

                if (check(entry))
                {
                    if (pairEntry.Value is FolderEntry)
                        entries.Insert(0, entry);
                    else
                        entries.Add(entry);
                }
                if (entry is FolderEntry)
                    (entry as FolderEntry).GetEntriesByConiditions(check, entries);

            }
            return entries;
        }


        public int GetTotalSize()
        {
            Dictionary<string, FileEntry> files = new Dictionary<string, FileEntry>();
            GetSubFileEntries(files);

            int totalSize = 0;
            foreach (KeyValuePair<string, FileEntry> pair in files)
            {
                totalSize += pair.Value.MaxBundleEntry().Length;
            }

            return totalSize;
        }
    }
}
