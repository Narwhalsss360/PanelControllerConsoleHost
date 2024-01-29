using CLIApplication;
using PanelController.Controller;
using PanelController.PanelObjects;
using PanelController.Profiling;
using System.Reflection;

namespace ConsoleHost
{
    public static class CLI
    {
        public static CLIInterpreter Interpreter = new(Show, Create, Select, Edit, Remove, LogDump, Clear, Quit)
        {
            InterfaceName = "PanelController",
            EntryMarker = ">"
        };

        #region Back API
        public static IPanelObject? CreateInstance(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            int selectedIndex = 0;

            if (constructors.Length == 0)
            {
                Console.WriteLine($"No constructors exist, {type.GetItemName()} is an invalid extension type");
                return null;
            }

            if (constructors.Length != 1)
            {
                Console.WriteLine("Select constructor:");
                for (int i = 0; i < constructors.Length; i++)
                    Console.WriteLine($"{i} {constructors[i].GetParameters().GetParametersDescription()}");
                if (!int.TryParse(Console.ReadLine(), out selectedIndex))
                {
                    Console.WriteLine("Selection was not a number");
                    return null;
                }
            }

            if (0 >  selectedIndex && constructors.Length <= selectedIndex)
            {
                Console.WriteLine("Index was out of bounds");
                return null;
            }

            ParameterInfo[] paramters = constructors[selectedIndex].GetParameters();
            object?[] arguments = Array.Empty<object?>();

            if (paramters.Length != 0)
            {

            }

            IPanelObject? @object = null;
            try
            {
                @object = Activator.CreateInstance(type, arguments) as IPanelObject;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured trying to create {type.GetItemName()}, {e.Message}");
                return null;
            }
            return @object;
        }

        public static IPanelObject? CreateDispatchedInstance(Type type)
        {
            IPanelObject? instance = null;
            Program.MainDispatcher.Invoke(() => { instance = CreateInstance(type); });
            return instance;
        }
        #endregion

        #region Show
        public static void ShowLoadedExtensions()
        {
            if (Extensions.AllExtensions.Length == 0)
                return;
            Console.WriteLine("Loaded Extensions:");
            foreach (Type extensions in Extensions.AllExtensions)
                Console.WriteLine($"    {extensions.GetItemName()} {extensions.FullName}");
        }

        public static void ShowProfiles()
        {
            if (Main.Profiles.Count == 0)
                return;
            Console.WriteLine("Profiles:");
            foreach (Profile profile in Main.Profiles)
                Console.WriteLine($"    {profile.Name} {(ReferenceEquals(Main.CurrentProfile, profile) ? "SELECTED" : "")}");
        }

        public static void ShowMappings()
        {
            if (Main.CurrentProfile is null)
                return;
            if (Main.CurrentProfile.Mappings.Length == 0)
                return;

            Console.WriteLine("Mappings: ");
            foreach (Mapping mapping in Main.CurrentProfile.Mappings)
            {
                Console.WriteLine($"    {mapping.PanelGuid.PanelInfoOrGuid()} {mapping.InterfaceType} ID:{mapping.InterfaceID} OPTION:{mapping.InterfaceOption}");
                foreach (Mapping.MappedObject mapped in mapping.Objects)
                    Console.WriteLine($"        {mapped.Object} {mapped.Delay} {mapped.Value}");
            }
        }

        public static void ShowPanels()
        {
            Console.WriteLine("Panels:");
            foreach (PanelInfo info in Main.PanelsInfo)
            {
                Console.WriteLine($"    {info.Name} {info.PanelGuid} {(info.IsConnected ? "CONNECTED" : "DISCONNECTED")}");
                Console.WriteLine($"        Digital Count:{info.DigitalCount}");
                Console.WriteLine($"        Analog Count:{info.AnalogCount}");
                Console.WriteLine($"        Display Count:{info.DisplayCount}");
            }
        }

        public enum ShowOptions
        {
            All,
            LoadedExtensions,
            Profiles,
            Mappings,
            Panels
        }

        public static void Show(ShowOptions option = ShowOptions.All)
        {
            switch (option)
            {
                case ShowOptions.All:
                    ShowLoadedExtensions();
                    ShowProfiles();
                    ShowMappings();
                    ShowPanels();
                    break;
                case ShowOptions.LoadedExtensions:
                    ShowLoadedExtensions();
                    break;
                case ShowOptions.Profiles:
                    ShowProfiles();
                    break;
                case ShowOptions.Mappings:
                    ShowMappings();
                    break;
                case ShowOptions.Panels:
                    ShowPanels();
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Create
        public static void CreateGeneric(string fullName)
        {
            if (fullName.FindType(Extensions.ExtensionCategories.Generic) is not Type type)
            {
                Console.WriteLine($"Extension {fullName}");
                return;
            }

            if (CreateDispatchedInstance(type) is not IPanelObject @object)
                return;

            Extensions.Objects.Add(@object);
        }

        public static void CreateProfile(string name, bool set = false)
        {
            Main.Profiles.Add(new() { Name = name });
            if (set)
                Main.SelectedProfileIndex = Main.Profiles.Count - 1;
        }

        public static void CreateMapping(string panelName)
        {
            if (Main.CurrentProfile is null)
            {
                Console.WriteLine("No current selected profile");
                return;
            }

            if (panelName.FindPanelInfo() is not PanelInfo info)
            {
                Console.WriteLine($"Did not find panel with name {panelName}");
                return;
            }

            Console.WriteLine("Enter interfaceType, interfaceID, onActivate(Digital Only)");
            if (Console.ReadLine() is not string entry)
                return;

            object?[] arguments = new Type[]
            {
                typeof(InterfaceTypes),
                typeof(uint),
                typeof(bool?)
            }.ParseArguments(entry.DeliminateOutside().ToArray(), new() { { 2, null } });

            bool? onActivate = arguments[2] as bool?;
            if (arguments[0] is not InterfaceTypes interfaceType || arguments[1] is not uint interfaceID)
            {
                Console.WriteLine("Invalid arguments entered");
                return;
            }

            if (Main.CurrentProfile.FindMapping(info.PanelGuid, interfaceType, interfaceID, onActivate) is not null)
            {
                Console.WriteLine("Mapping already exists");
                return;
            }

            Main.CurrentProfile.AddMapping(new()
            {
                PanelGuid = info.PanelGuid,
                InterfaceType = interfaceType,
                InterfaceID = interfaceID,
                InterfaceOption = onActivate
            });
        }

        public static void CreateMappedObject(string panelName)
        {
            if (Main.CurrentProfile is null)
            {
                Console.WriteLine("No current selected profile");
                return;
            }

            if (panelName.FindPanelInfo() is not PanelInfo info)
            {
                Console.WriteLine("Panel not found");
                return;
            }

            Console.WriteLine("Enter interfaceType, interfaceID, objectName, onActivate(Digital Only)");
            if (Console.ReadLine() is not string entry)
                return;

            object?[] arguments = new Type[]
            {
                typeof(InterfaceTypes),
                typeof(uint),
                typeof(string),
                typeof(bool?)
            }.ParseArguments(entry.DeliminateOutside().ToArray(), new() { { 2, null } });

            bool? onActivate = arguments[3] as bool?;
            if (arguments[0] is not InterfaceTypes interfaceType ||
                arguments[1] is not uint interfaceID ||
                arguments[2] is not string typeName)
            {
                Console.WriteLine("Invalid arguments entered");
                return;
            }

            if (Main.CurrentProfile.FindMapping(info.PanelGuid, interfaceType, interfaceID, onActivate) is not Mapping mapping)
            {
                Console.WriteLine("Mapping doesnt exist");
                return;
            }

            if (typeName.FindType() is not Type type)
            {
                Console.WriteLine("Type not found");
                return;
            }

            if (CreateInstance(type) is not IPanelObject @object)
                return;

            mapping.Objects.Add(new(@object, TimeSpan.Zero, null));
        }

        public static void CreatePanelInfo(string panelName)
        {
            if (panelName.FindPanelInfo() is not null)
            {
                Console.WriteLine("Panel with name already exsits");
                return;
            }

            Main.PanelsInfo.Add(new() { Name = panelName });
        }

        public enum CreateOptions
        {
            Generic,
            Profile,
            Mapping,
            MappedObject,
            PanelInfo
        }

        public static void Create(CreateOptions option, string name, string[]? flags = null)
        {
            switch (option)
            {
                case CreateOptions.Generic:
                    CreateGeneric(name);
                    break;
                case CreateOptions.Profile:
                    CreateProfile(name, flags is not null && flags.Contains("--select"));
                    break;
                case CreateOptions.Mapping:
                    CreateMapping(name);
                    break;
                case CreateOptions.MappedObject:
                    CreateMappedObject(name);
                    break;
                case CreateOptions.PanelInfo:
                    CreatePanelInfo(name);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Select
        public enum SelectOptions
        {
            Profile,
            Panel
        }

        public static object? ContainingObject = null;
        public static ICollection<object>? ContainingCollection = null;
        public static object? SelectedObject = null;

        public static void SelectProfile(string? name = null)
        {
            if (name is null)
            {
                Console.WriteLine("Select index:");
                for (int i = 0; i < Main.Profiles.Count; i++)
                    Console.WriteLine($"{i} {Main.Profiles[i].Name}");

                if (!int.TryParse(Console.ReadLine(), out int index))
                {
                    Console.WriteLine("Not a number");
                    return;
                }
                Main.SelectedProfileIndex = index;
                ContainingObject = null;
                SelectedObject = Main.CurrentProfile;
            }
            else
            {
                for (int i = 0; i < Main.Profiles.Count; i++)
                {
                    if (Main.Profiles[i].Name == name)
                    {
                        Main.SelectedProfileIndex = i;
                        ContainingObject = null;
                        SelectedObject = Main.CurrentProfile;
                        return;
                    }
                }
                Console.WriteLine($"Profile {name} not found");
                SelectProfile(null);
            }
        }

        public static void SelectPanel(string? panelName = null)
        {
            if (panelName is null)
            {
                Console.WriteLine("Select index:");
                for (int i = 0; i < Main.PanelsInfo.Count; i++)
                    Console.WriteLine($"{i} {Main.PanelsInfo[i].Name} | {Main.PanelsInfo[i].PanelGuid}");

                if (!int.TryParse(Console.ReadLine(), out int index))
                {
                    Console.WriteLine("Not a number");
                    return;
                }
                ContainingObject = null;
                SelectedObject = Main.PanelsInfo[index];
            }
            else
            {
                if (panelName.FindPanelInfo() is PanelInfo panelInfo)
                {
                    ContainingObject = null;
                    SelectedObject = panelInfo;
                    return;
                }
                Console.WriteLine($"Panel with name {panelName} was not found");
                SelectPanel(null);
            }
        }

        public static void Select(SelectOptions? option = null, string? name = null)
        {
            if (option is null)
            {
                Console.WriteLine("Currently selected:");
                Console.WriteLine($"    Containing object:{ContainingObject}");
                Console.WriteLine($"    Containing collection:{ContainingCollection}");
                Console.WriteLine($"    Selected object:{SelectedObject}");
                return;
            }
            switch (option)
            {
                case SelectOptions.Profile:
                    SelectProfile(name);
                    break;
                case SelectOptions.Panel:
                    SelectPanel(name);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Edit
        public enum EditOptions
        {
            Name
        }

        public static void EditName(string newName)
        {
            if (SelectedObject is Profile profile)
            {
            }
            else if (SelectedObject is PanelInfo panelInfo)
            {
                panelInfo.Name = newName;
            }
            else if (SelectedObject is Mapping mapping)
            {
            }
            else
            {
                Console.WriteLine("Cannot edit name of selected object");
            }
        }

        public static void Edit(EditOptions option, string name)
        {
            EditName(name);
        }
        #endregion

        #region Remove
        public enum RemoveOptions
        {

        }

        public static void Remove(RemoveOptions option)
        {

        }
        #endregion

        #region Other
        public static void LogDump(string format = "/T [/L][/F] /M")
        {
            foreach (var log in Logger.Logs)
                Console.WriteLine(log.ToString(format));
        }

        public static void Clear()
        {
            Console.Clear();
        }

        public static void Quit()
        {
            Program.Quit();
        }
        #endregion
    }
}
