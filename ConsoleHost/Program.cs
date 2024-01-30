using PanelController.Controller;
using PanelController.Profiling;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using Windows.System.Profile;
using CtrlMain = PanelController.Controller.Main;

namespace ConsoleHost
{
    public static class Program
    {
        public static Thread InterpreterThread = new(() => { CLI.Interpreter.Run(CtrlMain.DeinitializedCancellationToken); });

        public static System.Timers.Timer SaveTimer = new() { AutoReset = true, Interval = 10000 };

        public static readonly string CWD = Environment.CurrentDirectory;

        public static readonly string ExtensionsFolder = "Extensions";

        public static readonly string ExtensionsDirectory = Path.Combine(CWD, ExtensionsFolder);

        public static readonly string ProfilesFolder = "Profiles";

        public static readonly string ProfilesDirectory = Path.Combine(CWD, ProfilesFolder);

        public static readonly string PanelsInfoFolder = "PanelsInfo";

        public static readonly string PanelsInfoDirectory = Path.Combine(CWD, PanelsInfoFolder);

        public static readonly XmlWriterSettings MainXmlWriterSettings = new () { Indent = true, Encoding = System.Text.Encoding.UTF32, IndentChars = "    " };

        public static Dispatcher MainDispatcher = Dispatcher.CurrentDispatcher;

        public static void LoadExtensions()
        {
            if (!Directory.Exists(ExtensionsDirectory))
                return;
            Logger.Log($"Loading extensions from {ExtensionsDirectory}", Logger.Levels.Info, "Program");
            foreach (var file in Directory.GetFiles(ExtensionsDirectory))
            {
                if (!file.EndsWith(".dll"))
                    return;
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(file);
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
                Extensions.Load(assembly);
            }
        }

        public static void LoadProfiles()
        {
            if (!Directory.Exists(ProfilesDirectory))
                return;

            XmlSerializer serializer = new(typeof(Profile.SerializableProfile));
            foreach (FileInfo file in new DirectoryInfo(ProfilesDirectory).GetFiles())
            {
                if (file.Extension.ToLower() != ".xml")
                    continue;

                using FileStream stream = file.OpenRead();
                using XmlReader reader = XmlReader.Create(stream);
                if (!serializer.CanDeserialize(reader))
                    continue;
                if (serializer.Deserialize(reader) is not Profile.SerializableProfile serializable)
                    continue;

                CtrlMain.Profiles.Add(new(serializable));
            }
        }

        public static void LoadPanels()
        {
            if (!Directory.Exists(PanelsInfoDirectory))
                return;

            XmlSerializer serializer = new(typeof(PanelInfo));
            foreach (FileInfo file in new DirectoryInfo(PanelsInfoDirectory).GetFiles())
            {
                if (file.Extension.ToLower() != ".xml")
                    continue;

                using FileStream stream = file.OpenRead();
                using XmlReader reader = XmlReader.Create(stream);

                if (!serializer.CanDeserialize(reader))
                    continue;
                if (serializer.Deserialize(reader) is not PanelInfo panelInfo)
                    continue;

                CtrlMain.PanelsInfo.Add(panelInfo);
            }
        }

        public static void LoadAll()
        {
            LoadExtensions();
            LoadPanels();
            LoadProfiles();
        }

        public static void SaveProfiles()
        {
            if (!Directory.Exists(ProfilesDirectory))
                Directory.CreateDirectory(ProfilesDirectory);

            XmlSerializer serializer = new(typeof(Profile.SerializableProfile));
            foreach (Profile profile in CtrlMain.Profiles)
            {
                using FileStream file = File.Open(Path.Combine(ProfilesDirectory, $"{profile.Name}.xml"), FileMode.Create);
                using XmlWriter writer = XmlWriter.Create(file, MainXmlWriterSettings);
                serializer.Serialize(writer, new Profile.SerializableProfile(profile));
            }

            foreach (FileInfo file in new DirectoryInfo(ProfilesDirectory).GetFiles())
            {
                if (CtrlMain.Profiles.Any(profile => $"{profile.Name}.xml" == file.Name))
                    continue;
                file.Delete();
            }
        }

        public static void SavePanels()
        {
            if (!Directory.Exists(PanelsInfoDirectory))
                Directory.CreateDirectory(PanelsInfoDirectory);

            XmlSerializer serializer = new(typeof(PanelInfo));
            foreach (PanelInfo panelInfo in CtrlMain.PanelsInfo)
            {
                using FileStream file = File.Open(Path.Combine(PanelsInfoDirectory, $"{panelInfo.PanelGuid}.xml"), FileMode.Create);
                using XmlWriter writer = XmlWriter.Create(file, MainXmlWriterSettings);
                serializer.Serialize(writer, panelInfo);
            }

            foreach (FileInfo file in new DirectoryInfo(PanelsInfoDirectory).GetFiles())
            {
                if (CtrlMain.PanelsInfo.Any(panelInfo => $"{panelInfo.PanelGuid}.xml" == file.Name))
                    continue;
                file.Delete();
            }
        }

        public static void SaveAll(object? sender = null, EventArgs? args = null)
        {
            MainDispatcher.Invoke(() =>
            {
                try
                {
                    SaveProfiles();
                    SavePanels();
                }
                catch (InvalidOperationException e)
                {
                    if (!e.Message.Contains("Collection was modified; enumeration operation may not execute"))
                        throw;
                    return;
                }
            });
        }

        [STAThread]
        private static void Main()
        {
            SaveTimer.Elapsed += SaveAll;
            CtrlMain.Initialized += Initialized;
            CtrlMain.Deinitialized += Deinitalized;
            CtrlMain.Initialize();
        }

        public static void Initialized(object? sender, EventArgs args)
        {
            LoadAll();
            SaveTimer.Start();
            InterpreterThread.Start();
            if (CtrlMain.Profiles.Count > 0)
                CtrlMain.SelectedProfileIndex = 0;
            Dispatcher.Run();
        }

        public static void Deinitalized(object? sender, EventArgs args)
        {
            MainDispatcher.Invoke(() => { MainDispatcher.DisableProcessing(); });
            Dispatcher.ExitAllFrames();
            SaveTimer.Stop();
            CLI.Interpreter.Stop();
            Environment.Exit(0);
        }

        public static void Quit()
        {
            Console.WriteLine("Exiting...");
            SaveAll();
            CtrlMain.Deinitialize();
        }
    }
}