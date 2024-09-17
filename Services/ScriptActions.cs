using CSScripting;
using CSScriptLib;
using DieselBundleViewer.Models;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace DieselBundleViewer.Services
{
    public static class ScriptActions
    {
        public static Dictionary<string, Dictionary<string, FormatConverter>> Converters = [];
        public static List<Script> Scripts = [];
        public static ScriptEngine PythonEngine;

        public static string ScriptsFolder = "Scripts";

        static ScriptActions()
        {
            LoadScripts();
        }

        public static void LoadScripts()
        {
            if (!Directory.Exists(ScriptsFolder))
                return;

            var folders = Directory.EnumerateDirectories(ScriptsFolder);
            
            if (!folders.Any())
                return;

            Stopwatch watch = new();
            watch.Start();
            Console.WriteLine("Attempting to load scripts...");

            foreach (string folder in folders)
            {
                try
                {
                    string main_path;
                    if (File.Exists(main_path = Path.Combine(folder, "main.py")))
                    {
                        Console.WriteLine(".???");
                        PythonEngine ??= Python.CreateEngine();

                        dynamic scope = PythonEngine.CreateScope();
                        foreach (string file in Directory.GetFiles(folder))
                        {
                            PythonEngine.ExecuteFile(file, scope);
                        }

                        scope.register?.Invoke();
                    }
                    else if (File.Exists(main_path = Path.Combine(folder, "main.cs")))
                    {
                        Console.WriteLine(Directory.GetFiles(folder)[0]);
                        foreach (string file in Directory.GetFiles(folder))
                        {
                            try
                            {
                                Assembly objAssembly = CSScript.Evaluator.CompileCode(File.ReadAllText(file));
                                dynamic obj = objAssembly.CreateObject("main");
                                if (obj.GetType().GetMethod("register") != null)
                                    obj.register();
                            } catch(Exception e)
                            {
                                Console.WriteLine("Failed loading script at path {0}. Make sure you have a main class with a register method inside.", folder);
                                Console.WriteLine(e.Message);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Failed loading script at path {0}:", folder);
                    Console.WriteLine(exc.Message);
                    Console.WriteLine(exc.StackTrace);
                }

                watch.Stop();
            }
            Console.WriteLine("Loaded scripts successfully. It took {0} seconds", watch.ElapsedMilliseconds / 1000f);
        }


        public static void AddConverter(FormatConverter format)
        {
            if (format.Key == null)
            {
                Console.WriteLine("[ERROR] Converter must have a key variable!");
                return;
            }

            if (!Converters.ContainsKey(format.Type))
                Converters.Add(format.Type, []);

            if (Converters[format.Type].ContainsKey(format.Key))
            {
                Console.WriteLine("[ERROR] Conveter is already registered with key {0}", format.Key);
                return;
            }

            Converters[format.Type].Add(format.Key, format);
        }

        public static void RegisterConverter(dynamic pis)
        {
            AddConverter(new FormatConverter
            {
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

            Console.WriteLine("Registered script {0} ", pis.key);

            Scripts.Add(new Script(pis.title, pis));
        }

        public static FormatConverter GetConverter(string type, string key)
        {
            if (!Converters.TryGetValue(type, out Dictionary<string, FormatConverter> value) || !value.ContainsKey(key))
                return null;

            return value[key];
        }
    }

}
