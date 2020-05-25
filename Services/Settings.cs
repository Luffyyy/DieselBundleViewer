using AdonisUI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DieselBundleViewer.Services
{
    public class SettingsData
    {
        public bool DisplayEmptyFiles = false;
        public bool ExtractFullDir = false;
        public bool DarkMode = true;
        public List<string> RecentFiles = new List<string>();
    }

    public static class Settings
    {
        private const string FILE = "./Data/Settings.xml";
        public static SettingsData Data { get; set; }

        static Settings()
        {
            Console.WriteLine("Loading setting...");
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
                } catch (Exception e)
                {
                    Console.WriteLine("Error while reading settings file: "+e.Message);
                }
                if(Data == null)
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
            } catch (Exception e)
            {
                Console.WriteLine("Failed saving settings " + e.Message);
            }
        }
    }
}
