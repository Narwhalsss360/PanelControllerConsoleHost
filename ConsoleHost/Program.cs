using CLIApplication;
using PanelController.Controller;
using PanelController.PanelObjects.Properties;
using System.Net.Http.Headers;

CLIInterpreter interpreter = new(ShowLoadedExtensions, Clear, Quit);

#region Back API
#endregion

#region Commands
#region General
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

void Clear()
{
    Console.Clear();
}

void Quit()
{
    Main.Deinitialize();
}
#endregion
#endregion

Main.Initialize();
interpreter.Run(Main.DeinitializedCancellationToken);