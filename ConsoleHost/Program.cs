using CLIApplication;
using PanelController.Controller;
using PanelController.PanelObjects.Properties;
using PanelController.Profiling;
using System.Reflection;

const string ExtensionsFolder = "Extensions";

CLIInterpreter interpreter = new(
    ShowLoadedExtensions,
    CreateProfile,
    ShowProfiles,
    SelectProfile,
    CreateMapping,
    ShowMappings,
    DeleteMapping,
    DeleteProfile,
    Clear,
    Quit);

#region Back API
string ExtensionsDirectory = Path.Combine(Environment.CurrentDirectory, ExtensionsFolder);

void LoadFromDirectory()
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
    }
}
#endregion

#region Commands
void ShowLoadedExtensions(Extensions.ExtensionCategories? category = null)
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

void CreateProfile(string name, string[] flags)
{
    Main.Profiles.Add(new() { Name = name });
    if (flags.Contains("--s"))
        Main.SelectedProfileIndex = Main.Profiles.Count - 1;
}

void ShowProfiles()
{
    foreach (var item in Main.Profiles)
        Console.WriteLine(item);
}

void SelectProfile()
{
    if (Main.Profiles.Count == 0)
    {
        Console.WriteLine("No profiles.");
        return;
    }

    Console.WriteLine("Select profile!");
    for (int i = 0; i < Main.Profiles.Count; i++)
        Console.WriteLine($"{i} {Main.Profiles[i].Name}");

    int index;
    if (!int.TryParse(Console.ReadLine(), out index))
    {
        Console.WriteLine("Was not a number");
        return;
    }

    Main.SelectedProfileIndex = index;
}

void DeleteProfile()
{
    if (Main.SelectedProfileIndex < 0)
    {
        Console.WriteLine("No profile selected!");
        return;
    }

    Main.Profiles.RemoveAt(Main.SelectedProfileIndex);
}

void CreateMapping(string panelName, InterfaceTypes interfaceType, uint interfaceID, bool? activate = null)
{
    if (Main.CurrentProfile is null)
    {
        Console.WriteLine("No selected profile!");
        return;
    }

    if (Main.PanelsInfo.Find(panel => panel.Name == panelName) is not PanelInfo panelInfo)
    {
        Console.WriteLine($"Couldn't find panel with name {panelName}");
        return;
    }

    if (Main.CurrentProfile.FindMapping(panelInfo.PanelGuid, interfaceType, interfaceID, activate) is not null)
    {
        Console.WriteLine("Mapping already exists");
        return;
    }

    Main.CurrentProfile.AddMapping(new Mapping()
    {
        PanelGuid = panelInfo.PanelGuid,
        InterfaceType = interfaceType,
        InterfaceID = interfaceID,
        InterfaceOption = activate
    });
}

void DeleteMapping(string panelName, InterfaceTypes interfaceType, uint interfaceID, bool? activate = null)
{
    if (Main.CurrentProfile is null)
    {
        Console.WriteLine("No selected profile!");
        return;
    }

    if (Main.PanelsInfo.Find(panel => panel.Name == panelName) is not PanelInfo panelInfo)
    {
        Console.WriteLine($"Couldn't find panel with name {panelName}");
        return;
    }

    if (Main.CurrentProfile.FindMapping(panelInfo.PanelGuid, interfaceType, interfaceID, activate) is Mapping mapping)
    {
        Main.CurrentProfile.RemoveMapping(mapping);
        Console.WriteLine("Done.");
        return;
    }

    Console.WriteLine("Mapping doesn't exist!");
}

void ShowMappings()
{
    if (Main.CurrentProfile is null)
    {
        Console.WriteLine("No profile selected!");
        return;
    }

    foreach (var mapping in Main.CurrentProfile.Mappings)
    {
        Console.WriteLine($"PanelGuid:{mapping.PanelGuid}");
        Console.WriteLine($"InterfaceType:{mapping.InterfaceType}");
        Console.WriteLine($"InterfaceID:{mapping.InterfaceID}");
        Console.WriteLine($"InterfaceOption:{mapping.InterfaceOption}");

        foreach (var obj in mapping.Objects)
            Console.WriteLine($"    {obj.GetItemName()}: {obj.Item2} {obj.Item3}");
    }
}

void Clear()
{
    Console.Clear();
}

void Quit()
{
    Main.Deinitialize();
}
#endregion

Main.Initialize();
Task loadTask = Task.Run(() => LoadFromDirectory()).ContinueWith((t) =>
{
    if (t.IsFaulted)
    {
        Console.WriteLine("There was an error loading extensions!");
        Quit();
    }
    else
    {
        Console.WriteLine("Successfully loaded extensions.");
    }
});
interpreter.Run(Main.DeinitializedCancellationToken);