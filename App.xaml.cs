using Prism.Ioc;
using DieselBundleViewer.Views;
using System.Windows;
using System.Runtime.InteropServices;
using DieselEngineFormats.Bundle;
using DieselBundleViewer.ViewModels;
using DieselBundleViewer.Services;
using System.IO;
using System;
using DieselEngineFormats;
using DieselEngineFormats.ScriptData;
using System.Text;
using System.Collections.Generic;
using AdonisUI;
using WwiseSoundLib;
using Orangelynx.Multimedia;
using System.Windows.Threading;

namespace DieselBundleViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();

        public App()
        {
#if !DEBUG
            Dispatcher.UnhandledException += OnException;
            if(File.Exists("debug"))
#endif
            AllocConsole();


            Console.WriteLine("Loading local hashlist");
            if (File.Exists("Data/hashlist"))
                HashIndex.LoadParallel("Data/hashlist");
            else
                Console.WriteLine("Local hashlist is missing!");

            LoadConverters();
        }

        void OnException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An error has occurred: \n {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<ConvertFileDialog, ConvertFileDialogViewModel>();
            containerRegistry.RegisterDialog<UpdateHashlistDialog, UpdateHashlistDialogViewModel>();
            containerRegistry.RegisterDialog<AboutDialog, AboutDialogViewModel>();
            containerRegistry.RegisterDialog<SettingsDialog, SettingsDialogViewModel>();
            containerRegistry.RegisterDialog<FindDialog, FindDialogViewModel>();
            containerRegistry.RegisterDialog<BundleSelectorDialog, BundleSelectorDialogViewModel>();
            containerRegistry.RegisterDialog<PropertiesDialog, PropertiesViewModel>();
            containerRegistry.RegisterDialog<ProgressDialog, ProgressDialogViewModel>();

            containerRegistry.RegisterDialogWindow<DialogWindow>();
        }

        private void LoadConverters()
        {
            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "script_cxml",
                Title = "Custom XML",
                Extension = "xml",
                ExportEvent = (MemoryStream ms, bool escape) =>
                {
                    try
                    {
                        Dictionary<string, object> root = new ScriptData(new BinaryReader(ms), Utils.IsRaid()).Root;
                        return new CustomXMLNode("table", root, "").ToString(0, escape);
                    } catch (Exception e)
                    {
                        MessageBox.Show($"Failed to read scriptdata: \n {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                },
                Type = "scriptdata"
            });

            //Temporary until I get source code of this DLL, hopefully.
            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "stream",
                Title = "Stream to Wav",
                RequiresAttention = false,
                Extension = "wav",
                Type = "stream",
                SaveEvent = (Stream stream, string toPath) =>
                {
                    WavFile file = new WavFile(stream);
                    WavProcessor.ConvertToPCM(file);
                    file.WriteFile(toPath);
                },
            });

            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "diesel_strings",
                Title = "Diesel",
                Extension = "strings",
                ImportEvent = (path) => new StringsFile(path),
                Type = "strings"
            });

            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "movie",
                Title = "Bink Video",
                Extension = "bik",
                Type = "movie",
                RequiresAttention = false
            });

            //Loop each XML format to have it automatically get .xml suffix

            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "xmL_conversion",
                Type = "text",
                Extension = "xml",
                RequiresAttention = false
            });

            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "texture_dds",
                Title = "DDS",
                Extension = "dds",
                Type = "texture",
                RequiresAttention = false
            });

            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "strings_csv",
                Title = "CSV",
                Extension = "csv",
                ExportEvent = (MemoryStream ms, bool arg0) =>
                {
                    //Excel doesn't seem to like it?
                    StringsFile str = new StringsFile(ms);
                    StringBuilder builder = new StringBuilder();
                    builder.Append("ID,String\n");
                    foreach (var entry in str.LocalizationStrings)
                        builder.Append("\"" + entry.ID.ToString() + "\",\"" + entry.Text + "\"\n");
                    Console.WriteLine(builder.ToString());
                    return builder.ToString();
                },
                Type = "strings"
            });

            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "script_json",
                Title = "JSON",
                Extension = "json",
                ExportEvent = (MemoryStream ms, bool arg0) =>
                {
                    try
                    {
                        ScriptData sdata = new ScriptData(new BinaryReader(ms), Utils.IsRaid());
                        return (new JSONNode("table", sdata.Root, "")).ToString();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Failed to read scriptdata: \n {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                },
                Type = "scriptdata"
            });

            ScriptActions.AddConverter(new FormatConverter
            {
                Key = "strings_json",
                Title = "JSON",
                Extension = "json",
                ExportEvent = (MemoryStream ms, bool arg0) =>
                {
                    StringsFile str = new StringsFile(ms);
                    StringBuilder builder = new StringBuilder();
                    builder.Append("{\n");
                    for (int i = 0; i < str.LocalizationStrings.Count; i++)
                    {
                        StringEntry entry = str.LocalizationStrings[i];
                        builder.Append('\t');
                        builder.Append("\"" + entry.ID + "\" : \"" + entry.Text + "\"");
                        if (i < str.LocalizationStrings.Count - 1)
                            builder.Append(',');
                        builder.Append('\n');
                    }
                    builder.Append('}');
                    Console.WriteLine(builder.ToString());
                    return builder.ToString();
                },
                Type = "strings"
            });
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }
    }
}
