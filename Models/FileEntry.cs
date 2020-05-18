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
    public class FileEntry : IEntry, IChild
    {
        private string _name, _fullpath;
        private PackageFileEntry _max_entry = null;

        public Idstring _path;
        public Idstring _language;
        public Idstring _extension;

        private uint _size;
        public uint Size { 
            get {
                if (_size == 0 && BundleEntries.Count > 0)
                {
                    foreach (PackageFileEntry be in BundleEntries)
                        _size += (uint)be.Length;

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
                    _fullpath = _path.ToString();

                    if (_language != null)
                        _fullpath += "." + _language.ToString();

                    _fullpath += "." + _extension.ToString();
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
                {
                    _name = Path.GetFileName(_path.ToString());
                    if (_language != null)
                        _name += "." + _language.ToString();

                    _name += "." + _extension.ToString();
                }

                return _name;
            }

            set => _name = value;
        }

        public List<PackageFileEntry> BundleEntries = new List<PackageFileEntry>();

        public DatabaseEntry DBEntry { get; set; }

        public MainWindowViewModel DataContext { get; set; }

        public string SizeStr
        {
            get
            {
                uint size = Size;
                string str_size;
                if (size < 1024)
                    str_size = size.ToString() + " B";
                else
                    str_size = string.Format("{0:n0}", size / 1024) + " KB";

                return str_size;
            }
        }

        public string Type => _extension?.ToString();

        public string TempPath { get; set; }

        public PackageFileEntry TempEntry { get; set; }
        public IParent Parent { get; set; }

        public FileEntry() {
        }

        public FileEntry(DatabaseEntry dbEntry, PackageDatabase db, MainWindowViewModel dataContext) {
            DataContext = dataContext;
            SetDBEntry(dbEntry, db);
        }

        public void AddBundleEntry(PackageFileEntry entry)
        {
            BundleEntries.Add(entry);
            _max_entry = null;
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

            string bundle_path = Path.Combine(DataContext.AssetsDir, entry.Parent.Name.HashedString + ".bundle");
            Console.WriteLine(DataContext.AssetsDir);
            if (!File.Exists(bundle_path))
            {
                Console.WriteLine("Bundle: {0}, does not exist", bundle_path);
                return null;
            }

            try
            {
                using (FileStream fs = new FileStream(bundle_path, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        if (entry.Length != 0)
                        {
                            fs.Position = entry.Address;
                            return br.ReadBytes((int)(entry.Length == -1 ? fs.Length - fs.Position : entry.Length));
                        }
                        else
                            return new byte[0];
                        
                    }
                }
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

        public void SetDBEntry(DatabaseEntry ne, PackageDatabase db)
        {
            DBEntry = ne;
            General.GetFilepath(ne, out _path, out _language, out _extension, db);
        }

        public PackageFileEntry MaxBundleEntry()
        {
            if (BundleEntries.Count == 0)
                return null;

            if (_max_entry == null)
            {
                _max_entry = null;
                foreach (PackageFileEntry entry in BundleEntries)
                {
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
