using DieselBundleViewer.Models;
using DieselBundleViewer.ViewModels;
using DieselEngineFormats.Bundle;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DialogResult = System.Windows.Forms.DialogResult;

namespace DieselBundleViewer.Services
{
    public static class FileManager
    {
        public class TempFile : IDisposable
        {

            public string FilePath { get; set; }
            public Process RunProcess { get; set; }
            public PackageFileEntry Entry { get; set; }
            public string ExporterKey { get; set; }
            public bool Disposed { get; set; }

            public TempFile(FileEntry entry, PackageFileEntry be = null, FormatConverter converter = null, string filePath = null, bool includeInnerPath=true)
            {
                string path;
                if (includeInnerPath)
                    path = entry.EntryPath.Replace("/", "\\");
                else
                    path = entry.Name;

                if(filePath == null)
                {
                    FilePath = Path.Combine(Path.GetTempPath(), "DBV", path);
                    if (converter != null && converter.Extension != null)
                        FilePath += "." + converter.Extension;
                } else
                   FilePath = filePath;

                SaveFile(entry, FilePath, converter, be, true);

                Entry = be;
                if (converter != null)
                    ExporterKey = converter.Key;
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

        private static Dictionary<FileEntry, TempFile> TempFiles = new Dictionary<FileEntry, TempFile>();

        public static bool Disposed { get; set; }

        public static void UpdateTempDirectory()
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

        private static bool IsFileAvailable(string path)
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

        private static bool ShouldDeleteFile(TempFile file)
        {
            return file.RunProcess != null && file.RunProcess.HasExited && IsFileAvailable(file.FilePath);
        }

        private static void Update(object sender, EventArgs e)
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

        public static void ConvertFile(FileEntry entry, Action<FormatConverter> done)
        {
            if (entry.BundleEntries.Count == 0)
            {
                Console.WriteLine("No bundle entries.");
                return;
            }

            string typ = Definitions.TypeFromExtension(entry.ExtensionIds.ToString());

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
                        done(format);
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
                        done(selected.Title == "None" ? null : selected);
                    }
                });
            } else
            {
                done(null);
            }
        }

        public static void SaveFileConvert(FileEntry entry)
        {
            SaveFileDialog sfd = new SaveFileDialog { FileName = entry.Name };

            string typ = Definitions.TypeFromExtension(entry.ExtensionIds.ToString());
            if (ScriptActions.Converters.ContainsKey(typ))
            {
                var convs = ScriptActions.Converters[typ];
                string filter = "";
                var conerters = new List<FormatConverter>();
                foreach (var pair in convs)
                {
                    FormatConverter conv = pair.Value;
                    filter += $"{conv.Title} (*.{conv.Extension})|*.{conv.Extension}|";
                    conerters.Add(conv);
                }
                sfd.Filter = filter.Remove(filter.Length - 1); // + "|All files (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                    SaveFile(entry, sfd.FileName, conerters[sfd.FilterIndex - 1]);
            }
        }

        public static void SaveFileAs(FileEntry file)
        {
            SaveFileDialog sfd = new SaveFileDialog { FileName = file.Name, Filter = "All files (*.*)|*.*" };
            if (sfd.ShowDialog() == DialogResult.OK)
                SaveFile(file, sfd.FileName);
        }

        public static void SaveFile(FileEntry file, string path, FormatConverter converter=null, PackageFileEntry be = null, bool checkIfExists=false)
        {
            if (checkIfExists && File.Exists(path))
                return;

            object file_data = file.FileData(be, converter);

            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (file_data is byte[] v)
                File.WriteAllBytes(path, v);
            else if (file_data is Stream fs)
            {
                if (converter != null && converter.SaveEvent != null)
                    converter.SaveEvent(fs, path);
                else
                {
                    using (FileStream file_stream = File.Create(path))
                        fs.CopyTo(file_stream);

                    fs.Close();
                }
            }
            else if (file_data is string dataStr)
                File.WriteAllText(path, dataStr);
            else if (file_data is string[] dataArr)
                File.WriteAllLines(path, dataArr);
        }

        public static void SaveFolder(FolderEntry folder)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                var children = folder.GetAllChildren();

                var pms = new DialogParameters();
                var agg = new EventAggregator();
                var evnt = agg.GetEvent<SetProgressEvent>();
                pms.Add("SetProgress", evnt);

                pms.Add("TaskThread", new Thread(o =>
                {
                    Thread.Sleep(100); //Give dialog time to show up

                    var dialog = o as ProgressDialogViewModel;

                    for (int i = 0; i < children.Count; i++)
                    {
                        if (dialog.IsClosed)
                            return;

                        IEntry entry = children[i];
                        string path = Path.Combine(fbd.SelectedPath, entry.EntryPath.Replace("/", "\\"));
                        if (entry is FileEntry childFile)
                        {
                            evnt.Publish(new Tuple<string, float>($"Copying {entry.EntryPath}", 100 * (i / (float)children.Count)));
                            SaveFile(childFile, path);
                        }
                        else if (entry is FolderEntry childFolder)
                        {
                            Directory.CreateDirectory(path);
                        }
                    }
                    evnt.Publish(new Tuple<string, float>($"Done!", 100));
                }) { IsBackground = true });

                Utils.ShowDialog("ProgressDialog", pms);
            }
        }

        public static void ViewFile(FileEntry entry, PackageFileEntry be = null)
        {
            ConvertFile(entry, converter => DoViewFile(entry, be, converter));
        }

        public static void DoViewFile(FileEntry entry, PackageFileEntry be = null, FormatConverter exporter=null)
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

        public static TempFile CreateTempFile(FileEntry entry, PackageFileEntry be = null, FormatConverter exporter = null)
        {
            if (TempFiles.ContainsKey(entry))
                DeleteTempFile(entry);

            TempFile temp = new TempFile(entry, be, exporter);

            return temp;
        }

        public static TempFile GetTempFile(FileEntry file, PackageFileEntry entry = null, FormatConverter exporter = null)
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

        public static void DeleteTempFile(FileEntry entry)
        {
            if (!TempFiles.ContainsKey(entry))
                return;

            TempFile temp_file = TempFiles[entry];

            temp_file.Dispose();

            TempFiles.Remove(entry);
        }

        public static void DeleteAllTempFiles()
        {
            Console.WriteLine("Deleting temporary files.");
            List<TempFile> key_list = TempFiles.Values.ToList();
            for (int i = 0; i < TempFiles.Count; i++)
            {
                key_list[i].Dispose();
            }

            TempFiles.Clear();
        }
    }

}
