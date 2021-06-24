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

        /// <summary>
        /// Like SavePath but only name without full path.
        /// </summary>
        public string SaveName => Path.GetFileName(SavePath);

        /// <summary>
        /// Gets the path of the file for saving purposes.
        /// </summary>
        public string SavePath
        {
            get
            {
                var extReplacers = Settings.Data.ExtReplacers;
                bool append = Settings.Data.ExtReplacerAppend;
                foreach (var replacer in extReplacers)
                {
                    if (replacer[0] == Type)
                    {
                        if (append)
                            return EntryPath + "." + replacer[1];
                        else
                            return EntryPath.Replace(Type, replacer[1]);
                    }
                }
                return EntryPath;
            }
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

        /// <summary>
        /// Checks if the file is in a bundle.
        /// </summary>
        /// <param name="name">The name (idstring) of the bundle</param>
        /// <returns>true if it's in the bundle</returns>
        public bool InBundle(Idstring name) => BundleEntries.ContainsKey(name);

        /// <summary>
        /// Returns whether or not the file is in one of the bundles provided in the arguments
        /// </summary>
        /// <param name="names">Names (idstring) of packages to test with</param>
        public bool InBundles(List<Idstring> names)
        {
            foreach (var bundle in names)
            {
                if (BundleEntries.ContainsKey(bundle))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether or not a file has any data to extract. Cooked physics are ignored since they often are 0 bytes but still exist.
        /// </summary>
        /// <returns>true if the file has data</returns>
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

        /// <summary>
        /// Returns the bytes[] of the file
        /// </summary>
        /// <param name="entry">A package entry to use for the data. Defaults to what MaxBundleEntry returns.</param>
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

        /// <summary>
        /// Returns a MemoryStream of the file.
        /// </summary>
        /// <param name="entry">A package entry to use for the data. Defaults to what MaxBundleEntry returns.</param>
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

        /// <summary>
        /// Returns the bundle that has the largest version of the file.
        /// </summary>
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
