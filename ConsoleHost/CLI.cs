using CLIApplication;
using PanelController.Controller;
using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using PanelController.Profiling;
using System.Collections;
using System.Reflection;
i
namespace ConsoleHost
{
    public static class CLI
    {
        public static CLIInterpreter Interpreter = new(Show, Create, Select, Deselect, Edit, Remove, LogDump, Clear, Quit)
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

            if (0 > selectedIndex && constructors.Length <= selectedIndex)
            {
                Console.WriteLine("Index was out of bounds");
                return null;
            }

            ParameterInfo[] paramters = constructors[selectedIndex].GetParameters();
            object?[] arguments = Array.Empty<object?>();

            if (paramters.Length != 0)
            {

            }

            IPanelObject? @object;
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

        #region Select
        public static object? ContainingObject = null;
        public static IList? ContainingList = null;
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
                ContainingList = Main.Profiles;
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
                        ContainingList = Main.Profiles;
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
                ContainingList = Main.PanelsInfo;
                SelectedObject = Main.PanelsInfo[index];
            }
            else
            {
                if (Main.PanelsInfo.Find(info => info.Name == panelName) is PanelInfo panelInfo)
                {
                    ContainingObject = null;
                    ContainingList = Main.PanelsInfo;
                    SelectedObject = panelInfo;
                    return;
                }
                Console.WriteLine($"Panel with name {panelName} was not found");
                SelectPanel(null);
            }
        }

        public static void SelectGeneric()
        {
            if (Extensions.Objects.Count == 0)
            {
                Console.WriteLine("No objects");
                return;
            }

            Console.WriteLine("Select Index:");
            for (int i = 0; i < Extensions.Objects.Count; i++)
                Console.WriteLine($"{i} {Extensions.Objects[i].GetItemName()}");

            if (!int.TryParse(Console.ReadLine(), out int index))
            {
                Console.WriteLine("Not a number");
                return;
            }

            ContainingObject = null;
            ContainingList = Extensions.Objects;
            SelectedObject = Extensions.Objects[index];
        }

        public enum SelectOptions
        {
            Profile,
            Panel,
            Generic
        }

        public static void Select(SelectOptions? option = null, string? name = null)
        {
            if (option is null)
            {
                Console.WriteLine("Currently selected:");
                Console.WriteLine($"    Containing object:{ContainingObject}");
                Console.WriteLine($"    Containing collection:{ContainingList}");
                Console.WriteLine($"    Selected object:{SelectedObject}");
                Console.WriteLine($"Current Profile: {Main.CurrentProfile?.Name}");
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
                case SelectOptions.Generic:
                    SelectGeneric();
                    break;
                default:
                    break;
            }
        }

        public static void Deselect()
        {
            ContainingObject = null;
            ContainingList = null;
            SelectedObject = null;
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
                Console.WriteLine($"    {mapping.PanelGuid.PanelInfoNameOrGuid()} {mapping.InterfaceType} ID:{mapping.InterfaceID} OPTION:{mapping.InterfaceOption}");
                foreach (Mapping.MappedObject mapped in mapping.Objects)
                    Console.WriteLine($"        {mapped.Object}:{mapped.Object.Status} {mapped.Delay} {mapped.Value}");
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

        public static void ShowProperties()
        {
            if (SelectedObject is not IPanelObject @object)
            {
                Console.WriteLine("Property listing is only supported on types of IPanelObject");
                return;
            }

            PropertyInfo[] properties = @object.GetUserProperties();
            if (properties.Length == 0)
                return;

            Console.WriteLine($"{@object.GetItemName()}:");
            foreach (PropertyInfo property in properties)
                Console.WriteLine($"    {property.PropertyType.Name} {property.Name} = {property.GetValue(@object)}");
        }

        public enum ShowOptions
        {
            All,
            LoadedExtensions,
            Profiles,
            Mappings,
            Panels,
            Properties,
            Selected
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
                case ShowOptions.Properties:
                    ShowProperties();
                    break;
                case ShowOptions.Selected:
                    Select(null);
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

            if (Main.PanelsInfo.Find(info => info.Name == panelName) is not PanelInfo info)
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

            PanelInfo info;
            if (Main.PanelsInfo.Find(info => info.Name == panelName) is PanelInfo fromName)
            {
                info = fromName;
            }
            else
            {
                if (Main.PanelsInfo.Count == 0)
                {
                    Console.WriteLine("No panels info, please create one or connect a panel");
                    return;
                }

                Console.WriteLine("Select Index:");
                for (int i = 0; i < Main.PanelsInfo.Count; i++)
                    Console.WriteLine($"    {i} {Main.PanelsInfo[i].Name} {Main.PanelsInfo[i].PanelGuid}");

                if (!int.TryParse(Console.ReadLine(), out int index))
                {
                    Console.WriteLine("Not a number");
                    return;
                }

                info = Main.PanelsInfo[index];
            }

            Console.WriteLine("Enter panelName/Guid, interfaceType, interfaceID, objectName, onActivate(Digital Only)");
            if (Console.ReadLine() is not string entry)
                return;

            object?[] arguments = new Type[]
            {
                typeof(string),
                typeof(InterfaceTypes),
                typeof(uint),
                typeof(string),
                typeof(bool?)
            }.ParseArguments(entry.DeliminateOutside().ToArray(), new() { { 4, null } });

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
            if (Main.PanelsInfo.Find(info => info.Name == panelName) is not null)
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

        #region Edit
        public enum EditOptions
        {
            Name,
            PanelInfo,
            Property
        }

        public static void EditName(string name)
        {
            if (SelectedObject is Profile profile)
                profile.Name = name;
            else if (SelectedObject is PanelInfo panelInfo)
                panelInfo.Name = name;
            else if (SelectedObject is Mapping mapping)
                mapping.Name = name;
            else if (SelectedObject is IPanelObject panelObject)
                panelObject.TrySetItemName(name);
            else
                Console.WriteLine("Cannot edit name of selected object");
        }

        public static void EditPanelInfo(string valueEntry)
        {
            if (SelectedObject is not PanelInfo info)
            {
                Console.WriteLine("Selected object is not PanelInfo");
                return;
            }

            if (!valueEntry.Contains('='))
            {
                Console.WriteLine($"{nameof(EditPanelInfo)} syntax: property=valueEntry");
                return;
            }
            string property = valueEntry[..valueEntry.IndexOf('=')];
            valueEntry = valueEntry[(valueEntry.IndexOf('=') + 1)..];

            if (!uint.TryParse(valueEntry, out uint value))
            {
                Console.WriteLine("Not a number");
                return;
            }

            if (property == "DigitalCount")
                info.DigitalCount = value;
            else if (property == "AnalogCount")
                info.AnalogCount = value;
            else if (property == "DisplayCount")
                info.DisplayCount = value;
            else
                Console.WriteLine($"Property {property} not found");
        }

        public static void EditProperty(string valueEntry)
        {
            if (SelectedObject is not IPanelObject @object)
            {
                Console.WriteLine("Selected object is not IPanelObject");
                return;
            }

            if (!valueEntry.Contains('='))
            {
                Console.WriteLine($"{nameof(EditProperty)} syntax: property=valueEntry");
                return;
            }
            string property = valueEntry[..valueEntry.IndexOf('=')];
            valueEntry = valueEntry[(valueEntry.IndexOf('=') + 1)..];

            if (Array.Find(@object.GetUserProperties(), prop => prop.Name == property) is not PropertyInfo propertyInfo)
            {
                Console.WriteLine($"Property {property} not found");
                return;
            }

            if (!ParameterInfoExtensions.IsSupported(propertyInfo.PropertyType))
            {
                Console.WriteLine($"Invalid Extension, type {propertyInfo.PropertyType} is not supported");
                return;
            }

            if (valueEntry.ParseAs(propertyInfo.PropertyType) is not object @value)
            {
                Console.WriteLine($"There was an error parsing {valueEntry}");
                return;
            }

            propertyInfo.SetValue(@object, @value);
        }

        public static void Edit(EditOptions option, string value)
        {
            switch (option)
            {
                case EditOptions.Name:
                    EditName(value);
                    break;
                case EditOptions.PanelInfo:
                    EditPanelInfo(value);
                    break;
                case EditOptions.Property:
                    EditProperty(value);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Remove
        public static void Remove()
        {
            Console.WriteLine("Are you sure? y/n");
            if (Console.ReadLine() is not string input)
                return;
            if (!input.ToLower().StartsWith("y"))
                return;

            if (ContainingList is null)
            {
                Console.WriteLine("Remove: Must finalize, unkown list");
                return;
            }

            ContainingList.Remove(SelectedObject);
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
