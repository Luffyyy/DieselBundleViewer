using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DieselBundleViewer.Services
{
    public class SettingsData
    {
        public bool DisplayEmptyFiles { get; set; } = false;
        public bool ExtractFullDir { get; set; } = false;
        public bool DarkMode { get; set; } = true;
        public List<string> RecentFiles = [];
        public DateTime HaslistLastUpdate { get; set; }
    }

    public static class Settings
    {
        private const string FILE = "./Data/Settings.xml";
        public static SettingsData Data { get; set; }

        static Settings()
        {
            Console.WriteLine("Loading settings...");
            ReadSettings();
        }

        public static void ReadSettings()
        {
            if (!File.Exists(FILE))
                SaveSettings();
            else
            {
                XmlSerializer xml = new XmlSerializer(typeof(SettingsData));
                using var fs = new FileStream(FILE, FileMode.Open, FileAccess.Read);
                try
                {
                    Data = (SettingsData)xml.Deserialize(fs);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error while reading settings file: " + e.Message);
                }
                if (Data == null)
                {
                    Console.WriteLine("Settings file is corrupted. Creating a new one.");
                    SaveSettings();
                }
            }
        }

        public static void SaveSettings()
        {
            Data ??= new SettingsData();

            string dir = Path.GetDirectoryName(FILE);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            XmlSerializer xml = new XmlSerializer(typeof(SettingsData));
            try
            {
                using var fs = new FileStream(FILE, FileMode.Create, FileAccess.Write);
                xml.Serialize(fs, Data);
                Console.WriteLine("Saved settings");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed saving settings " + e.Message);
            }
        }
    }
}
