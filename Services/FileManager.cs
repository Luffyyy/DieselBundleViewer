using DieselBundleViewer.Models;
using DieselBundleViewer.ViewModels;
using DieselEngineFormats.Bundle;
using Prism.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                GC.SuppressFinalize(this);

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

            if (ScriptActions.Converters.TryGetValue(typ, out Dictionary<string, FormatConverter> value))
            {
                var convs = value;

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
                formats.Add(new FormatConverter { Title = "None" });

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

        public static void SaveFileConvert(FileEntry file, string removeDirectory)
        {
            SaveFileDialog sfd = new SaveFileDialog { FileName = file.Name };

            string typ = Definitions.TypeFromExtension(file.ExtensionIds.ToString());
            if (ScriptActions.Converters.TryGetValue(typ, out Dictionary<string, FormatConverter> value))
            {
                var convs = value;
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
                {
                    string filePath = Path.Combine(Utils.GetDirectory(file.EntryPath), sfd.FileName);
                    if (!Settings.Data.ExtractFullDir && !string.IsNullOrEmpty(removeDirectory))
                        filePath = filePath.Replace(removeDirectory.Replace("/", "\\") + "\\", "");
                    SaveFile(file, filePath, conerters[sfd.FilterIndex - 1]);
                }
            }
        }

        public static void SaveFileAs(FileEntry file, string removeDirectory)
        {
            SaveFileDialog sfd = new SaveFileDialog { FileName = file.Name, Filter = "All files (*.*)|*.*" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string filePath = Path.Combine(Utils.GetDirectory(file.EntryPath), sfd.FileName);
                if (!Settings.Data.ExtractFullDir && !string.IsNullOrEmpty(removeDirectory))
                    filePath = filePath.Replace(removeDirectory.Replace("/", "\\") + "\\", "");
                SaveFile(file, filePath);
            }
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
                    using FileStream file_stream = File.Create(path);
                    fs.CopyTo(file_stream);
                    fs.Close();
                }
            }
            else if (file_data is string dataStr)
                File.WriteAllText(path, dataStr);
            else if (file_data is string[] dataArr)
                File.WriteAllLines(path, dataArr);
        }

        public static void SaveMultiple(List<IEntry> entries, string removeDirectory)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                var pms = new DialogParameters();
                pms.Add("ProgressAction", new Action<ProgressDialogViewModel>(dialog => {
                    Thread t = new Thread(o =>
                    {
                        Thread.Sleep(100); //Give dialog time to show up
                        int total = entries.Count;
                        for (int i = 0; i < total; i++)
                        {
                            if (dialog.IsClosed)
                                return;

                            IEntry entry = entries[i];
                            string entryPath = entry.EntryPath.Replace("/", "\\");
                            if (!Settings.Data.ExtractFullDir && !string.IsNullOrEmpty(removeDirectory))
                                entryPath = entryPath.Replace(removeDirectory.Replace("/", "\\") + "\\", "");

                            string path = Path.Combine(fbd.SelectedPath, entryPath);

                            if (entry is FileEntry childFile)
                            {
                                int current = i + 1;
                                dialog.SetProgress($"Copying {entry.EntryPath}", current, total);
                                SaveFile(childFile, path);
                            }
                            else if (entry is FolderEntry childFolder)
                            {
                                Directory.CreateDirectory(path);
                            }
                        }
                    });

                    t.IsBackground = true;
                    t.Start();
                }));

                Utils.ShowDialog("ProgressDialog", pms);
            }
        }

        public static async Task ExtractAll(string outputDir, List<IEntry> files, string removeDirectory, IProgress<ProgressRecord> progress, CancellationToken ct)
        {
            progress.Report(new ProgressRecord("Determining working order", 0, 0));

            var filesToProcess = files.OfType<FileEntry>()
                .Select<FileEntry, (FileEntry fe, PackageFileEntry pfe, string BundleName)>(fe =>
                {
                    var bh = fe.MaxBundleEntry();
                    return (fe, bh, bh.Parent.BundleName);
                })
                .GroupBy(i => i.BundleName)
                .ToDictionary(t => t.Key, t => t.OrderBy(e => e.pfe.Address).ToList());

            var dirsToCreate = files.OfType<FolderEntry>().OrderBy(fe => fe.EntryPath).ToList();
            int total = dirsToCreate.Count;
            int currNum = 0;
            foreach(var entry in dirsToCreate)
            {
                progress.Report(new ProgressRecord("Creating directory tree", total, currNum));

                string entryPath = entry.EntryPath.Replace("/", "\\");
                if (!Settings.Data.ExtractFullDir && !string.IsNullOrEmpty(removeDirectory))
                    entryPath = entryPath.Replace(removeDirectory.Replace("/", "\\") + "\\", "");
                string path = Path.Combine(outputDir, entryPath);
                Directory.CreateDirectory(path);

                currNum++;
            }

            total = filesToProcess.Select(kv => kv.Value.Count).Sum();
            currNum = 0;
            foreach (var (bundleName, fileList) in filesToProcess)
            {
                if (ct.IsCancellationRequested) { return; }

                string bundle_path = Path.Combine(Utils.CurrentWindow.AssetsDir, bundleName + ".bundle");
                if (!File.Exists(bundle_path))
                {
                    Console.WriteLine("Bundle: {0}, does not exist!", bundle_path);
                    continue;
                }

                if (fileList.Count == 0) { continue; }

                using var fs = new FileStream(bundle_path, FileMode.Open, FileAccess.Read, FileShare.Read, 16834, true);

                if (fileList.Select(i => i.pfe.Length).Sum() > (fs.Length / 2))
                {
                    progress.Report(new ProgressRecord("Reading bundle", total, currNum));
                    var entireBundle = new byte[fs.Length];
                    await fs.ReadAsync(entireBundle.AsMemory(0, (int)fs.Length), ct);

                    foreach (var (entry, pfe, bn) in fileList)
                    {
                        progress.Report(new ProgressRecord($"Copying {entry.EntryPath}", total, currNum));
                        if (ct.IsCancellationRequested) { return; }

                        string entryPath = entry.EntryPath.Replace("/", "\\");
                        if (!Settings.Data.ExtractFullDir && !string.IsNullOrEmpty(removeDirectory))
                            entryPath = entryPath.Replace(removeDirectory.Replace("/", "\\") + "\\", "");
                        string path = Path.Combine(outputDir, entryPath);

                        using var outfile = File.Create(path);
                        await outfile.WriteAsync(entireBundle.AsMemory((int)pfe.Address, (int)pfe.Length), ct);

                        currNum++;
                    }
                }
                else
                {
                    List<Task> writes = new List<Task>(fileList.Count);
                    foreach (var (entry, pfe, bn) in fileList)
                    {
                        progress.Report(new ProgressRecord($"Copying {entry.EntryPath}", total, currNum));

                        string entryPath = entry.EntryPath.Replace("/", "\\");
                        if (!Settings.Data.ExtractFullDir && !string.IsNullOrEmpty(removeDirectory))
                            entryPath = entryPath.Replace(removeDirectory.Replace("/", "\\") + "\\", "");
                        string path = Path.Combine(outputDir, entryPath);

                        using var outfile = File.Create(path);
                        var buf = new byte[pfe.Length];
                        fs.Seek(pfe.Address, SeekOrigin.Begin);
                        await fs.ReadAsync(buf.AsMemory(0, pfe.Length), ct);
                        await outfile.WriteAsync(buf.AsMemory(0, pfe.Length), ct);

                        currNum++;
                    }
                }
            }

            progress.Report(new ProgressRecord($"Done", total, total));
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
                TempFiles.TryAdd(entry, temp);
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
            if (!TempFiles.TryGetValue(file, out TempFile value) || value.Disposed || !File.Exists(value.FilePath) || (exporter != null && value.ExporterKey != exporter.Key) || value.Entry != entry)
            {
                if (TempFiles.ContainsKey(file))
                    DeleteTempFile(file);

                path = CreateTempFile(file, entry, exporter);
            }
            else
                path = value;

            return path;
        }

        public static void DeleteTempFile(FileEntry entry)
        {
            if (!TempFiles.TryGetValue(entry, out TempFile value))
                return;

            TempFile temp_file = value;

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
