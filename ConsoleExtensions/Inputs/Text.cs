using Microsoft.VisualBasic;
using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;

namespace ConsoleExtensions.Inputs
{
    public class Text : IPanelAction
    {
        [UserProperty]
        public string InputText { get; set; } = string.Empty;

        public object? Run()
        {
            InternalStatics.InputSimulator.Keyboard.TextEntry(InputText);
            return null;
        }
    }
}
