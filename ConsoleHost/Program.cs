using CLIApplication;
using PanelController.Controller;

CLIInterpreter interpreter = new();

#region Back API
#endregion

#region Commands
#region General
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