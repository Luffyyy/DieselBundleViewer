using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DieselBundleViewer.Services
{
    public class FormatConverter
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Extension { get; set; }
        public bool RequiresAttention { get; set; } = true;
        public string Type { get; set; }

        public delegate object ExportEventDel(MemoryStream ms, bool arg0);
        public ExportEventDel ExportEvent { get; set; }

        public object Export(MemoryStream ms, bool arg0 = false)
        {
            if (ExportEvent != null)
                return ExportEvent(ms, arg0);
            return ms;
        }

        public delegate object ImportEventDel(string path);
        public ImportEventDel ImportEvent { get; set; }

        public object Import(string path)
        {
            if (ImportEvent != null)
                return ImportEvent(path);
            return null;
        }
    }

    public static class ScriptActions
    {
        public static Dictionary<string, Dictionary<string, FormatConverter>> Converters = new Dictionary<string, Dictionary<string, FormatConverter>>();
        public static Dictionary<string, dynamic> Scripts = new Dictionary<string, dynamic>();

        public static void AddConverter(FormatConverter format)
        {
            if (format.Key == null)
            {
                Console.WriteLine("[ERROR] Converter must have a key variable!");
                return;
            }

            if (!Converters.ContainsKey(format.Type))
                Converters.Add(format.Type, new Dictionary<string, FormatConverter>());

            if (Converters[format.Type].ContainsKey(format.Key))
            {
                Console.WriteLine("[ERROR] Conveter is already registered with key {0}", format.Key);
                return;
            }

            Converters[format.Type].Add(format.Key, format);
        }

        public static void RegisterConverter(dynamic pis)
        {
            AddConverter(new FormatConverter {
                Key = pis.key,
                Extension = pis.extension,
                Type = pis.type,
                ExportEvent = pis.export,
                Title = pis.title
            });
        }

        public static void RegisterScript(dynamic pis)
        {
            if (pis.key == null)
            {
                Console.WriteLine("[ERROR] Script to register must have a key variable!");
                return;
            }

            if (Scripts.ContainsKey(pis.key))
            {
                Console.WriteLine("[ERROR] Script is already registered with key {1}", pis.key);
                return;
            }

            Scripts.Add(pis.key, pis);
        }

        public static FormatConverter GetConverter(string type, string key)
        {
            if (!Converters.ContainsKey(type) || !Converters[type].ContainsKey(key))
                return null;

            return Converters[type][key];
        }
    }
}
