﻿using CLIApplication;
using PanelController.Controller;
using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using PanelController.Profiling;
using System.Reflection;
using System.IO;
using CtrlMain = PanelController.Controller.Main;
using System.Windows.Threading;
using System.Windows;

public static class Program
{
    public static CLIInterpreter interpreter = new(
        ShowLoadedExtensions,
        CreateProfile,
        Dump,
        ShowProfiles,
        SelectProfile,
        CreateMapping,
        ShowMappings,
        AddToMapping,
        DeleteMapping,
        DeleteProfile,
        CreateObject,
        ShowConnectedPanels,
        SetPanelName,
        Clear,
        Quit)
    {
        InterfaceName = "PanelController CLI"
    };

    public static readonly string ExtensionsFolder = "Extensions";

    public static readonly string ExtensionsDirectory = Path.Combine(Environment.CurrentDirectory, ExtensionsFolder);

    public static void LoadFromDirectory()
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
   
    [STAThread]
    private static void Main(string[] args)
    {
        CtrlMain.Initialize();
        Task loadTask = Task.Run(() => LoadFromDirectory()).ContinueWith((t) =>
        {
            if (t.IsFaulted)
            {
                Console.WriteLine("There was an error loading extensions!");
                Quit();
            }
        });
        interpreter.Run(CtrlMain.DeinitializedCancellationToken);
    }

    public static void ShowLoadedExtensions(Extensions.ExtensionCategories? category = null)
    {
        if (category is null)
        {
            foreach (var item in Enum.GetValues<Extensions.ExtensionCategories>())
                ShowLoadedExtensions(item);
            return;
        }

        Console.WriteLine($"{category.Value}:");
        foreach (var item in Extensions.ExtensionsByCategory[category.Value])
            Console.WriteLine($"{item.GetItemName()}({item.FullName}):{item.GetItemDescription()}");
    }

    public static void Dump()
    {
        foreach (var item in Logger.Logs)
            Console.WriteLine($"{item:[/L][/F] /M}");
    }

    public static void CreateProfile(string name, string[]? flags = null)
    {
        CtrlMain.Profiles.Add(new() { Name = name });
        if (flags.Contains("--s"))
            CtrlMain.SelectedProfileIndex = CtrlMain.Profiles.Count - 1;
    }

    public static void ShowProfiles()
    {
        foreach (var item in CtrlMain.Profiles)
            Console.WriteLine(item);
    }

    public static void SelectProfile()
    {
        if (CtrlMain.Profiles.Count == 0)
        {
            Console.WriteLine("No profiles.");
            return;
        }

        Console.WriteLine("Select profile!");
        for (int i = 0; i < CtrlMain.Profiles.Count; i++)
            Console.WriteLine($"{i} {CtrlMain.Profiles[i].Name}");

        int index;
        if (!int.TryParse(Console.ReadLine(), out index))
        {
            Console.WriteLine("Was not a number");
            return;
        }

        CtrlMain.SelectedProfileIndex = index;
    }

    public static void DeleteProfile()
    {
        if (CtrlMain.SelectedProfileIndex < 0)
        {
            Console.WriteLine("No profile selected!");
            return;
        }

        CtrlMain.Profiles.RemoveAt(CtrlMain.SelectedProfileIndex);
    }

    public static void CreateMapping(string panelName, InterfaceTypes interfaceType, decimal interfaceID, bool? activate = null)
    {
        if (CtrlMain.CurrentProfile is null)
        {
            Console.WriteLine("No selected profile!");
            return;
        }

        if (CtrlMain.PanelsInfo.Find(panel => panel.Name == panelName) is not PanelInfo panelInfo)
        {
            Console.WriteLine($"Couldn't find panel with name {panelName}");
            return;
        }

        if (CtrlMain.CurrentProfile.FindMapping(panelInfo.PanelGuid, interfaceType, (uint)interfaceID, activate) is not null)
        {
            Console.WriteLine("Mapping already exists");
            return;
        }

        CtrlMain.CurrentProfile.AddMapping(new Mapping()
        {
            PanelGuid = panelInfo.PanelGuid,
            InterfaceType = interfaceType,
            InterfaceID = (uint)interfaceID,
            InterfaceOption = activate
        });
    }

    public static void DeleteMapping(string panelName, InterfaceTypes interfaceType, decimal interfaceID, bool? activate = null)
    {
        if (CtrlMain.CurrentProfile is null)
        {
            Console.WriteLine("No selected profile!");
            return;
        }

        if (CtrlMain.PanelsInfo.Find(panel => panel.Name == panelName) is not PanelInfo panelInfo)
        {
            Console.WriteLine($"Couldn't find panel with name {panelName}");
            return;
        }

        if (CtrlMain.CurrentProfile.FindMapping(panelInfo.PanelGuid, interfaceType, (uint)interfaceID, activate) is Mapping mapping)
        {
            CtrlMain.CurrentProfile.RemoveMapping(mapping);
            Console.WriteLine("Done.");
            return;
        }

        Console.WriteLine("Mapping doesn't exist!");
    }

    public static void ShowMappings()
    {
        if (CtrlMain.CurrentProfile is null)
        {
            Console.WriteLine("No profile selected!");
            return;
        }

        foreach (var mapping in CtrlMain.CurrentProfile.Mappings)
        {
            Console.WriteLine($"PanelGuid:{mapping.PanelGuid}");
            Console.WriteLine($"InterfaceType:{mapping.InterfaceType}");
            Console.WriteLine($"InterfaceID:{mapping.InterfaceID}");
            Console.WriteLine($"InterfaceOption:{mapping.InterfaceOption}");

            foreach (var obj in mapping.Objects)
                Console.WriteLine($"    {obj.GetItemName()}: {obj.Item2} {obj.Item3}");
        }
    }

    public static void AddToMapping(string panelName, InterfaceTypes interfaceType, decimal interfaceID, string typeFullName, bool? activate = null)
    {
        if (CtrlMain.CurrentProfile is null)
        {
            Console.WriteLine("No selected profile!");
            return;
        }

        if (CtrlMain.PanelsInfo.Find(panel => panel.Name == panelName) is not PanelInfo panelInfo)
        {
            Console.WriteLine($"Couldn't find panel with name {panelName}");
            return;
        }

        if (CtrlMain.CurrentProfile.FindMapping(panelInfo.PanelGuid, interfaceType, (uint)interfaceID, activate) is not Mapping mapping)
        {
            Console.WriteLine("Mapping doesnt exist!");
            return;
        }

        if (Array.Find(Extensions.AllExtensions, t => t.FullName == typeFullName) is not Type type)
        {
            Console.WriteLine("Type not found!");
            return;
        }

        IPanelObject? panelObject = CreateInstance(type);

        if (panelObject is null)
            return;

        mapping.Objects.Add(new(panelObject, TimeSpan.Zero, null));
    }

    private static IPanelObject? CreateInstance(Type type)
    {
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

        int index = 0;


        if (constructors.Length > 1)
        {
            Console.WriteLine("Select constructor:");
            for (int i = 0; i < constructors.Length; i++)
                Console.WriteLine($"{i} {constructors[i].Name}({constructors[i].GetParameters().GetParametersDescription()})");
            if (!int.TryParse(Console.ReadLine(), out index))
            {
                Console.WriteLine("Not a number!");
                return null;
            }
        }

        if (0 > index || constructors.Length <= index)
        {
            Console.WriteLine("Invalid index!");
            return null;
        }

        object?[] arguments = Array.Empty<object>();

        ConstructorInfo constructor = constructors[index];
        ParameterInfo[] parameters = constructor.GetParameters();

        if (parameters.Length != 0)
        {
            Console.Write("Enter Arguemnts:");
            if (Console.ReadLine() is not string entry)
                return null;

            string[] entries = entry.DeliminateOutside().ToArray();


            if (entries.Length < parameters.RequiredArguments())
            {
                Console.WriteLine("Not enough arguments.");
                return null;
            }

            arguments = parameters.ParseArguments(entries);
        }

        IPanelObject? panelObject;
        try
        {
            panelObject = Activator.CreateInstance(type, arguments) as IPanelObject;
        }
        catch (Exception e)
        {
            Console.WriteLine($"An exception was thrown creating the instance: {e.Message}.");
            return null;
        }

        if (panelObject is null)
        {
            Console.WriteLine("Object was not of IPanelObject.");
            return null;
        }

        return panelObject;
    }

    public static void CreateObject(string typeFullName)
    {
        if (Array.Find(Extensions.AllExtensions, t => t.FullName == typeFullName) is not Type type)
        {
            Console.WriteLine("Type not found!");
            return;
        }

        IPanelObject? panelObject = CreateInstance(type);

        if (panelObject is null)
            return;

        if (type.IsAssignableTo(typeof(Window)))
            Dispatcher.Run();

        Extensions.Objects.Add(panelObject);
    }

    public static void ShowConnectedPanels()
    {
        Console.WriteLine("Panel Giuid | Channel Name | Name");
        foreach (var item in CtrlMain.ConnectedPanels)
            Console.WriteLine($"{item.PanelGuid} | {item.Channel.GetItemName()} | {CtrlMain.PanelsInfo.Find(info => info.PanelGuid == item.PanelGuid)}");
    }

    public static void SetPanelName()
    {
        if (CtrlMain.ConnectedPanels.Count == 0)
        {
            Console.WriteLine("No panels are connected!");
            return;
        }

        Console.WriteLine("Select a panel:");
        Console.WriteLine("Select Index | Panel Giuid | Channel Name | Name");
        for (int i = 0; i < CtrlMain.ConnectedPanels.Count; i++)
        {
            var item = CtrlMain.ConnectedPanels[i];
            Console.WriteLine($"{i} | {item.PanelGuid} | {item.Channel.GetItemName()} | {CtrlMain.PanelsInfo.Find(info => info.PanelGuid == item.PanelGuid)}");
        }

        int index;
        if (!int.TryParse(Console.ReadLine(), out index))
        {
            Console.WriteLine("Not a number!");
            return;
        }

        PanelInfo info;
        if (CtrlMain.PanelsInfo.Find(info => info.PanelGuid == CtrlMain.ConnectedPanels[index].PanelGuid) is PanelInfo found)
        {
            info = found;
        }
        else
        {
            info = new PanelInfo() { PanelGuid = CtrlMain.ConnectedPanels[index].PanelGuid };
            CtrlMain.PanelsInfo.Add(info);
        }

        Console.Write("New name:");
        info.Name = Console.ReadLine() ?? "Panel";
    }

    public static void Clear()
    {
        Console.Clear();
    }

    public static void Quit()
    {
        Dispatcher.ExitAllFrames();
        CtrlMain.Deinitialize();
        Console.WriteLine("Exiting...");
    }
}