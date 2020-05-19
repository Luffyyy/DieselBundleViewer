using DieselBundleViewer.Models;
using DieselEngineFormats.Bundle;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DieselBundleViewer.Services
{
    public class TempFileManager : IDisposable
    {
        public class TempFile : IDisposable
        {

            public string FilePath { get; set; }
            public Process RunProcess { get; set; }
            public PackageFileEntry Entry { get; set; }
            public string ExporterKey { get; set; }
            public bool Disposed { get; set; }

            public TempFile(FileEntry entry, PackageFileEntry be = null, FormatConverter exporter = null)
            {
                string path = entry._path.UnHashed.Replace("/", "\\");
                FilePath = Path.Combine(Path.GetTempPath(), "DBV", $"{path}.{entry._extension.UnHashed}");

                if (exporter != null && exporter.Extension != null)
                    FilePath += "." + exporter.Extension;

                object file_data = entry.FileData(be, exporter);

                string dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (file_data is byte[])
                {
                    File.WriteAllBytes(FilePath, (byte[])file_data);
                }
                else if (file_data is Stream)
                {
                    using (FileStream file_stream = File.Create(FilePath))
                        ((Stream)file_data).CopyTo(file_stream);

                    ((Stream)file_data).Close();
                }
                else if (file_data is string)
                    File.WriteAllText(FilePath, (string)file_data);
                else if (file_data is string[])
                    File.WriteAllLines(FilePath, (string[])file_data);

                Entry = be;
                if (exporter != null)
                    ExporterKey = exporter.Key;
            }

            ~TempFile()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (Disposed)
                    return;

                try
                {

                    if (!(RunProcess?.HasExited ?? true))
                        RunProcess?.Kill();

                    if (File.Exists(FilePath))
                        File.Delete(FilePath);

                    Console.WriteLine("Deleted temp file {0}", FilePath);

                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }

                Disposed = true;
            }
        }

        private Dictionary<FileEntry, TempFile> TempFiles = new Dictionary<FileEntry, TempFile>();

        public bool Disposed { get; set; }

        public TempFileManager()
        {
            string temp_path;

            try
            {

                if (!Directory.Exists(temp_path = Path.Combine(Path.GetTempPath(), "DBV")))
                    Directory.CreateDirectory(temp_path);
                else
                {
                    foreach (string file in Directory.GetFiles(temp_path))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
        }

        ~TempFileManager()
        {
            this.Dispose();
        }

        private bool IsFileAvailable(string path)
        {
            try
            {
                using FileStream str = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return str.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool ShouldDeleteFile(TempFile file)
        {
            return file.RunProcess != null && file.RunProcess.HasExited && IsFileAvailable(file.FilePath);
        }

        private void Update(object sender, EventArgs e)
        {
            if (TempFiles.Count == 0)
                return;

            List<FileEntry> to_delete = new List<FileEntry>();
            foreach (var temp in TempFiles)
            {
                if (IsFileAvailable(temp.Value.FilePath))
                    to_delete.Add(temp.Key);
            }

            foreach (FileEntry ent in to_delete)
            {
                DeleteTempFile(ent);
            }
        }

        public void ViewFile(FileEntry entry, PackageFileEntry be = null)
        {
            if (entry.BundleEntries.Count == 0)
            {
                Console.WriteLine("No bundle entries.");
                return;
            }

            string typ = Definitions.TypeFromExtension(entry._extension.ToString());

            Console.WriteLine($"The type of the file {typ}");

            if (ScriptActions.Converters.ContainsKey(typ))
            {
                var convs = ScriptActions.Converters[typ];

                //Don't open the dialog for things that are just an extension change
                if(convs.Count == 1)
                {
                    FormatConverter format = convs.First().Value;
                    if (!format.RequiresAttention)
                    {
                        DoViewFile(entry, be, format);
                        return;
                    }
                }

                var formats = convs.Values.ToList();
                formats.Insert(0, new FormatConverter { Title = "None" });

                DialogParameters pms = new DialogParameters
                {
                    { "Formats", formats }
                };
                Utils.CurrentDialogService.ShowDialog("ConvertFileDialog", pms, r =>
                {
                    if (r.Result == ButtonResult.OK)
                    {
                        FormatConverter selected = pms.GetValue<FormatConverter>("Format");
                        DoViewFile(entry, be, selected.Title == "None" ? null : selected);
                    }
                });
            } else
            {
                DoViewFile(entry, be);
            }
        }
        public void DoViewFile(FileEntry entry, PackageFileEntry be = null, FormatConverter exporter=null)
        {
            try
            {
                if (entry.BundleEntries.Count == 0)
                    return;

                TempFile temp = GetTempFile(entry, be, exporter);
                GC.Collect();
                Process proc = new Process();
                proc.StartInfo.FileName = "explorer";
                proc.StartInfo.Arguments = $"\"{temp.FilePath}\"";
                proc.Start();
                if (!TempFiles.ContainsKey(entry))
                    TempFiles.Add(entry, temp);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
            }
        }

        public TempFile CreateTempFile(FileEntry entry, PackageFileEntry be = null, FormatConverter exporter = null)
        {
            if (TempFiles.ContainsKey(entry))
                DeleteTempFile(entry);

            TempFile temp = new TempFile(entry, be, exporter);

            return temp;
        }

        public TempFile GetTempFile(FileEntry file, PackageFileEntry entry = null, FormatConverter exporter = null)
        {
            TempFile path;
            if (!TempFiles.ContainsKey(file) || TempFiles[file].Disposed || !File.Exists(TempFiles[file].FilePath) || (exporter != null && TempFiles[file].ExporterKey != exporter.Key) || TempFiles[file].Entry != entry)
            {
                if (TempFiles.ContainsKey(file))
                    DeleteTempFile(file);

                path = CreateTempFile(file, entry, exporter);
            }
            else
                path = TempFiles[file];

            return path;
        }

        public void DeleteTempFile(FileEntry entry)
        {
            if (!TempFiles.ContainsKey(entry))
                return;

            TempFile temp_file = TempFiles[entry];

            temp_file.Dispose();

            TempFiles.Remove(entry);
        }

        public void DeleteAllTempFiles()
        {
            List<TempFile> key_list = TempFiles.Values.ToList();
            for (int i = 0; i < TempFiles.Count; i++)
            {
                key_list[i].Dispose();
            }

            TempFiles = new Dictionary<FileEntry, TempFile>();
        }

        public void Dispose()
        {
            if (Disposed)
                return;

            DeleteAllTempFiles();

            Disposed = true;
        }
    }

}
