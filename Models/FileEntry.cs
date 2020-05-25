using DieselBundleViewer.Services;
using DieselBundleViewer.ViewModels;
using DieselEngineFormats.Bundle;
using DieselEngineFormats.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace DieselBundleViewer.Models
{
    public class FileEntry : IEntry
    {
        private string _name, _fullpath;
        private PackageFileEntry _max_entry = null;

        public Idstring PathIds;
        public Idstring LanguageIds;
        public Idstring ExtensionIds;

        private uint _size;
        public uint Size {
            get {
                if (_size == 0 && BundleEntries.Count > 0)
                {
                    foreach (var be in BundleEntries)
                    {
                        _size += (uint)(be.Value).Length;
                    }

                    _size = (uint)(_size / Math.Max(BundleEntries.Count, 1));
                }
                return _size;
            }
        }

        public string EntryPath
        {
            get
            {
                if (_fullpath == null)
                {
                    _fullpath = PathIds.ToString();

                    if (LanguageIds != null)
                        _fullpath += "." + LanguageIds.ToString();

                    _fullpath += "." + ExtensionIds.ToString();
                }
                return _fullpath;
            }
            set => _fullpath = value;
        }

        public string Name
        {
            get
            {
                if (_name == null)
                    _name = Path.GetFileName(EntryPath);

                return _name;
            }

            set => _name = value;
        }

        public Dictionary<Idstring, PackageFileEntry> BundleEntries { get; set; }

        public DatabaseEntry DBEntry { get; set; }

        public string Type => ExtensionIds?.ToString();

        public FolderEntry Parent { get; set; }

        public FileEntry() {
            BundleEntries = new Dictionary<Idstring, PackageFileEntry>();
        }

        public FileEntry(DatabaseEntry dbEntry) : this() {
            DBEntry = dbEntry;
        }

        public void LoadPath()
        {
            if(DBEntry != null)
                General.GetFilepath(DBEntry, out PathIds, out LanguageIds, out ExtensionIds, DBEntry.Parent);
        }

        public void AddBundleEntry(PackageFileEntry entry)
        {
            if (!BundleEntries.ContainsKey(entry.PackageName))
            {
                BundleEntries.Add(entry.PackageName, entry);
                _max_entry = null;
            }
        }
        public bool InBundle(Idstring name) => BundleEntries.ContainsKey(name);

        public bool InBundles(List<Idstring> names)
        {
            foreach (var bundle in names)
            {
                if (BundleEntries.ContainsKey(bundle))
                    return true;
            }
            return false;
        }

        public bool HasData()
        {
            return Settings.Data.DisplayEmptyFiles || Type == "cooked_physics" || Size > 0;
        }

        public object FileData(PackageFileEntry be = null, FormatConverter exporter = null)
        {
           if (exporter == null)
                return FileStream(be);
           else
           {
                MemoryStream stream = FileStream(be);
                return stream == null ? null : exporter.Export(FileStream(be));
            }
        }

        private byte[] FileEntryBytes(PackageFileEntry entry)
        {
            if (entry == null)
            {
                Console.WriteLine("Entry null?");
                return null;
            }

            string bundle_path = Path.Combine(Utils.CurrentWindow.AssetsDir, entry.Parent.BundleName + ".bundle");
            if (!File.Exists(bundle_path))
            {
                Console.WriteLine("Bundle: {0}, does not exist", bundle_path);
                return null;
            }

            try
            {
                using FileStream fs = new FileStream(bundle_path, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(fs);
                if (entry.Length != 0)
                {
                    fs.Position = entry.Address;
                    return br.ReadBytes((int)(entry.Length == -1 ? fs.Length - fs.Position : entry.Length));
                }
                else
                    return new byte[0];
            }
            catch (Exception exc)
            {
                Console.WriteLine("FAIL");
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
            }

            return null;
        }

        public MemoryStream FileStream(PackageFileEntry entry = null)
        {
            entry ??= MaxBundleEntry();

            byte[] bytes = FileEntryBytes(entry);
            if (bytes == null)
                return null;

            MemoryStream stream = new MemoryStream(bytes) { Position = 0 };
            return stream;
        }

        public byte[] FileBytes(PackageFileEntry entry = null)
        {
            entry ??= MaxBundleEntry();

            return FileEntryBytes(entry);
        }

        public PackageFileEntry MaxBundleEntry()
        {
            if (BundleEntries.Count == 0)
                return null;

            if (_max_entry == null)
            {
                _max_entry = null;
                foreach (var pair in BundleEntries)
                {
                    PackageFileEntry entry = pair.Value;
                    if (_max_entry == null)
                    {
                        _max_entry = entry;
                        continue;
                    }

                    if (entry.Length > _max_entry.Length)
                        _max_entry = entry;
                }

            }

            return _max_entry;
        }
    }
}
