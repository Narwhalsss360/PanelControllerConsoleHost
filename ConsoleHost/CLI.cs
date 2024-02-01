using CLIApplication;
using PanelController.Controller;
using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using PanelController.Profiling;
using System.Collections;
using System.Reflection;

namespace ConsoleHost
{
    public static class CLI
    {
        public static CLIInterpreter Interpreter = new(Show, Create, Select, Deselect, Edit, Remove, LogDump, Clear, Program.SaveAll, Quit)
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
                Console.WriteLine(paramters.GetParametersDescription());
                Console.Write("Enter arguments:");
                string[]? entries;
                try
                {
                    entries = Console.ReadLine()?.DeliminateOutside().ToArray();
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e);
                    return null;
                }

                if (entries is null)
                    return null;

                arguments = paramters.ParseArguments(entries);
            }

            IPanelObject? @object;
            try
            {
                @object = Activator.CreateInstance(type, arguments) as IPanelObject;
            }
            catch (Exception? e)
            {
                Console.WriteLine($"An error occured trying to create {type.GetItemName()}, {e.Message}");
                e = e.InnerException;

                while (e is not null)
                {
                    Console.WriteLine($"Inner: {e}");
                    e = e.InnerException;
                }
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

        public static T? List<T>(this IList<T> enumerable) where T : class
        {
            int index = -1;

            if (enumerable.Count == 0)
                return null;

            Console.WriteLine("Select index:");
            for (int i = 0; i < enumerable.Count; i++)
                Console.WriteLine($"{i} {enumerable[i]}");

            if (!int.TryParse(Console.ReadLine(), out index))
                Console.WriteLine("Not a number");

            return index == -1 ? null : enumerable[index];
        }

        public static T? MatchOrList<T>(this IList<T> enumerable, Predicate<T>? predicate = null) where T : class
        {
            if (predicate is null)
                return enumerable.List();

            for (int i = 0; i < enumerable.Count; i++)
            {
                if (predicate(enumerable[i]))
                    return enumerable[i];
            }
            return enumerable.List();
        }
        #endregion

        #region Select

        /*
         * Valid Selectable Types:
         * - Generic: From Extensions.Objects[[]
         * - PanelInfo: From Main.PanelsInfo[]
         * - Profile: From Main.Profiles[]
         * - Mapping: From CurrentProfile.Mappings[]
         * - MappedObject: From Mapping.Objects, Mapping must be preselected
         * - IPanelObject: From MappedObject.Object, Mapping must be preselected and InnerObject argument given
         */

        public static object? ContainingObject = null;
        public static IList? ContainingList = null;
        public static object? SelectedObject = null;

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

        public static void SelectPanel(string? panelName = null)
        {
            if (panelName is null)
            {
                Console.WriteLine("Select index:");
                for (int i = 0; i < Main.PanelsInfo.Count; i++)
                    Console.WriteLine($"{i} {Main.PanelsInfo[i]}");

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
                Console.WriteLine($"Panel with profileName {panelName} was not found");
                SelectPanel(null);
            }
        }

        public static void SelectProfile(string? profileName = null)
        {
            if (profileName is null)
            {
                Console.WriteLine("Select index:");
                for (int i = 0; i < Main.Profiles.Count; i++)
                    Console.WriteLine($"{i} {Main.Profiles[i]}");

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
                    if (Main.Profiles[i].Name == profileName)
                    {
                        Main.SelectedProfileIndex = i;
                        ContainingObject = null;
                        ContainingList = Main.Profiles;
                        SelectedObject = Main.CurrentProfile;
                        return;
                    }
                }
                Console.WriteLine($"Profile {profileName} not found");
                SelectProfile(null);
            }
        }
        
        public static void SelectMapping(string? mappingName = null)
        {
            if (Main.CurrentProfile is null)
            {
                Console.WriteLine("No currently selected profile");
                return;
            }

            if (Main.CurrentProfile.Mappings.MatchOrList(mapping => mapping.Name == mappingName) is not Mapping mapping)
                return;

            ContainingObject = Main.CurrentProfile;
            ContainingList = Main.CurrentProfile.MappingsByGuid[mapping.PanelGuid];
            SelectedObject = mapping;
        }

        public static void SelectMappedObject(bool? inner = null)
        {
            inner ??= false;
            if (SelectedObject is Mapping.MappedObject mappedObject)
            {
                if (!inner.Value)
                    return;
                ContainingObject = mappedObject;
                ContainingList = null;
                SelectedObject = mappedObject.Object;
                return;
            }

            if (SelectedObject is not Mapping mapping)
            {
                Console.WriteLine("Selected type is not of mapping");
                return;
            }

            if (mapping.Objects.List() is not Mapping.MappedObject mapped)
            {
                Console.WriteLine("MappedObject not selected");
                return;
            }

            if (inner.Value)
            {
                ContainingObject = mapped;
                ContainingList = null;
                SelectedObject = mapped.Object;
            }
            else
            {
                ContainingObject = mapping;
                ContainingList = mapping.Objects;
                SelectedObject = mapped;
            }
        }

        public enum SelectOptions
        {
            Generic,
            Panel,
            Profile,
            Mapping,
            MappedObject
        }

        public static void Select(SelectOptions? option = null, string? value = null)
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
                case SelectOptions.Generic:
                    SelectGeneric();
                    break;
                case SelectOptions.Panel:
                    SelectPanel(value);
                    break;
                case SelectOptions.Profile:
                    SelectProfile(value);
                    break;
                case SelectOptions.Mapping:
                    SelectMapping(value);
                    break;
                case SelectOptions.MappedObject:
                    SelectMappedObject(value == "InnerObject");
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
                Console.WriteLine($"    {profile} {(ReferenceEquals(Main.CurrentProfile, profile) ? "SELECTED" : "")}");
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
                Console.WriteLine($"    {mapping}");
                foreach (Mapping.MappedObject mapped in mapping.Objects)
                    Console.WriteLine($"        {mapped.Object}:{mapped.Object.Status} {mapped.Delay} {mapped.Value}");
            }
        }

        public static void ShowPanels()
        {
            if (Main.PanelsInfo.Count == 0)
                return;

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
            if (SelectedObject is null)
            {
                Console.WriteLine("No");
                return;
            }

            Type[] knownTypes = new Type[]
            {
                typeof(Profile),
                typeof(PanelInfo),
                typeof(Mapping)
            };

            if (SelectedObject is not IPanelObject @object)
            {
                if (!knownTypes.Contains(SelectedObject.GetType()))
                    Console.WriteLine($"WARNING: Listing of non-IPanelObject, listing {SelectedObject.GetType().Name} {SelectedObject}");

                Console.WriteLine("Properties:");
                foreach (PropertyInfo property in SelectedObject.GetType().GetProperties())
                {
                    Console.Write($"    {(property.IsUserProperty() ? "*" : "")}{property.PropertyType.Name} {property.Name} = ");
                    try
                    {
                        Console.WriteLine(property.GetValue(SelectedObject)?.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception thrown trying to read: {e}");
                    }
                }

                Console.WriteLine("Fields:");
                foreach (FieldInfo field in SelectedObject.GetType().GetFields())
                {
                    Console.Write($"    {field.FieldType.Name} {field.Name} = ");
                    try
                    {
                        Console.WriteLine(field.GetValue(SelectedObject)?.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception thrown trying to read: {e}");
                    }
                }
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
                    Select(null);
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
        public static void CreateGeneric(string fullName, bool dispatched = false)
        {
            if (fullName.FindType(Extensions.ExtensionCategories.Generic) is not Type type)
            {
                Console.WriteLine($"Extension {fullName} is not Generic/Found");
                return;
            }

            IPanelObject? created = dispatched ? CreateDispatchedInstance(type) : CreateInstance(type);
            if (created is not IPanelObject @object)
                return;

            Extensions.Objects.Add(@object);
        }

        public static void CreateChannel(string fullName, bool dispatched = false)
        {
            if (fullName.FindType(Extensions.ExtensionCategories.Channel) is not Type type)
            {
                Console.WriteLine($"Type {fullName} not found");
                return;
            }

            IPanelObject? created = dispatched ? CreateDispatchedInstance(type) : CreateInstance(type);
            if (created is not IChannel channel)
            {
                Console.WriteLine("Instance was not IChannel");
                return;
            }

            _ = Main.HandshakeAsync(channel);
        }

        public static void CreateProfile(string name, bool set = false)
        {
            Main.Profiles.Add(new() { Name = name });
            if (set)
                Main.SelectedProfileIndex = Main.Profiles.Count - 1;
        }

        public static void CreateMapping(string name)
        {
            if (Main.CurrentProfile is null)
            {
                Console.WriteLine("No current selected profile");
                return;
            }

            Console.WriteLine("Enter interfaceType, interfaceID, panelName (enter '_' to select from list), onActivate(Digital Only)");
            if (Console.ReadLine() is not string entry)
                return;

            object?[] arguments = new Type[]
            {
                typeof(InterfaceTypes),
                typeof(uint),
                typeof(string),
                typeof(bool?)
            }.ParseArguments(entry.DeliminateOutside().ToArray(), new()
            {
                { 2, null },
                { 3, null }
            });

            if (arguments[0] is not InterfaceTypes interfaceType ||
                arguments[1] is not uint interfaceID)
            {
                Console.WriteLine("Invalid arguments entered");
                return;
            }
            bool? onActivate = arguments[3] as bool?;

            if (Main.PanelsInfo.MatchOrList(info => info.Name == arguments[2] as string ) is not PanelInfo info)
            {
                Console.WriteLine("PanelInfo not found");
                return;
            }

            Mapping newMapping = new() { Name = name, PanelGuid = info.PanelGuid, InterfaceType = interfaceType, InterfaceID = interfaceID, InterfaceOption = onActivate };

            if (Main.CurrentProfile.FindMapping(newMapping) is not null)
            {
                Console.WriteLine("Mapping already exists");
                return;
            }

            Main.CurrentProfile.AddMapping(newMapping);
        }

        public static void CreateMappedObject(string panelName, bool dispatched = false)
        {
            if (SelectedObject is not Mapping mapping)
            {
                Console.WriteLine("Selected object must be of tpe Mapping");
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

            Console.Write("Enter type name:");
            if (Console.ReadLine().FindType() is not Type type)
            {
                Console.WriteLine("Type not found");
                return;
            }

            IPanelObject? created = dispatched ? CreateDispatchedInstance(type) : CreateInstance(type);
            if (created is not IPanelObject @object)
                return;

            mapping.Objects.Add(new(@object, TimeSpan.Zero, null));
        }

        public static void CreatePanelInfo(string panelName)
        {
            if (Main.PanelsInfo.Find(info => info.Name == panelName) is not null)
            {
                Console.WriteLine("Panel with profileName already exsits");
                return;
            }

            Main.PanelsInfo.Add(new() { Name = panelName });
        }

        public enum CreateOptions
        {
            Generic,
            Channel,
            Profile,
            Mapping,
            MappedObject,
            PanelInfo
        }

        public static void Create(CreateOptions option, string name, string[]? flags = null)
        {
            flags ??= Array.Empty<string>();
            switch (option)
            {
                case CreateOptions.Generic:
                    CreateGeneric(name, flags.Contains("--dispatched"));
                    break;
                case CreateOptions.Channel:
                    CreateChannel(name, flags.Contains("--dispatched"));
                    break;
                case CreateOptions.Profile:
                    CreateProfile(name, flags.Contains("--select"));
                    break;
                case CreateOptions.Mapping:
                    CreateMapping(name);
                    break;
                case CreateOptions.MappedObject:
                    CreateMappedObject(name, flags.Contains("--dispatched"));
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
                Console.WriteLine("Cannot edit value of selected object");
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
                Console.WriteLine("Remove: unkown list");
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
