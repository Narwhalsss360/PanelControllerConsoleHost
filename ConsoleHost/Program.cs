using PanelController.Controller;
using PanelController.Profiling;
using System.Reflection;
using System.IO;
using CtrlMain = PanelController.Controller.Main;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.Xml;

namespace ConsoleHost
{
    public static class Program
    {
        public static Thread InterpreterThread = new Thread(() => { CLI.Interpreter.Run(CtrlMain.DeinitializedCancellationToken); });

        public static System.Timers.Timer SaveTimer = new() { AutoReset = true, Interval = 10000 };

        public static readonly string CWD = Environment.CurrentDirectory;

        public static readonly string ExtensionsFolder = "Extensions";

        public static readonly string ExtensionsDirectory = Path.Combine(CWD, ExtensionsFolder);

        public static readonly string ProfilesFolder = "Profiles";

        public static readonly string ProfilesDirectory = Path.Combine(CWD, ProfilesFolder);

        public static readonly string PanelsInfoFolder = "PanelsInfo";

        public static readonly string PanelsInfoDirectory = Path.Combine(CWD, PanelsInfoFolder);

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
            if (Directory.Exists(ProfilesDirectory))
            {
                XmlSerializer serializer = new(typeof(Profile.SerializableProfile));
                foreach (var file in new DirectoryInfo(ProfilesDirectory).GetFiles())
                {
                    if (file.Extension != ".xml")
                        continue;
                    using FileStream stream = file.OpenRead();
                    XmlReader reader = XmlReader.Create(stream);
                    if (!serializer.CanDeserialize(reader))
                        continue;

                    if (serializer.Deserialize(reader) is not Profile.SerializableProfile profile)
                        continue;
                    CtrlMain.Profiles.Add(new(profile));
                }
            }

            if (Directory.Exists(PanelsInfoDirectory))
            {
                XmlSerializer serializer = new(typeof(PanelInfo));
                foreach (var file in new DirectoryInfo(PanelsInfoDirectory).GetFiles())
                {
                    if (file.Extension != ".xml")
                        continue;
                    using FileStream stream = file.OpenRead();
                    XmlReader reader = XmlReader.Create(stream);
                    if (!serializer.CanDeserialize(reader))
                        continue;

                    if (serializer.Deserialize(reader) is not PanelInfo panelInfo)
                        continue;
                    CtrlMain.PanelsInfo.Add(panelInfo);
                }
            }
        }

        public static void SaveProfiles()
        {
            if (!Directory.Exists(ProfilesDirectory))
                Directory.CreateDirectory(ProfilesDirectory);

            XmlSerializer serializer = new(typeof(Profile.SerializableProfile));
            foreach (var profile in CtrlMain.Profiles)
            {
                string profilePath = Path.Combine(ProfilesDirectory, $"{profile.Name}.xml");
                try
                {
                    using FileStream file = File.Open(profilePath, FileMode.OpenOrCreate, FileAccess.Write);
                    serializer.Serialize(file, new Profile.SerializableProfile(profile));
                }
                catch (IOException)
                {
                    continue;
                }
            }

            foreach (var file in new DirectoryInfo(ProfilesDirectory).GetFiles())
            {
                if (CtrlMain.Profiles.Any(profile => $"{profile.Name}.xml" == file.Name))
                    continue;
                try
                {
                    file.Delete();
                }
                catch (IOException)
                {
                    continue;
                }
            }

            if (!Directory.Exists(PanelsInfoDirectory))
                Directory.CreateDirectory(PanelsInfoDirectory);

            serializer = new(typeof(PanelInfo));
            foreach (var info in CtrlMain.PanelsInfo)
            {
                string panelInfoPath = Path.Combine(PanelsInfoDirectory, $"{info.PanelGuid}.xml");
                try
                {
                    using FileStream stream = File.Open(panelInfoPath, FileMode.Create, FileAccess.Write);
                    serializer.Serialize(stream, info);
                }
                catch (IOException)
                {
                    continue;
                }
            }

            foreach (var file in new DirectoryInfo(PanelsInfoDirectory).GetFiles())
            {
                if (CtrlMain.PanelsInfo.Any(info => $"{info.PanelGuid}.xml" == file.Name))
                    continue;
                try
                {
                    file.Delete();
                }
                catch (IOException)
                {
                    continue;
                }
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            LoadExtensions();
            LoadProfiles();
            SaveTimer.Elapsed += (sender, args) => { SaveProfiles(); };
            InterpreterThread.SetApartmentState(ApartmentState.STA);

            CtrlMain.Initialize();
            SaveTimer.Start();
            InterpreterThread.Start();
            Dispatcher.Run();
        }

        public static void Quit()
        {
            Console.WriteLine("Exiting...");
            MainDispatcher.Invoke(() => { MainDispatcher.DisableProcessing(); });
            Dispatcher.ExitAllFrames();
            SaveTimer.Stop();
            CtrlMain.Deinitialize();
            CLI.Interpreter.Stop();
            Environment.Exit(0);
        }
    }
}