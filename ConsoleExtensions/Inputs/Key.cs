using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using WindowsInput.Native;

namespace ConsoleExtensions.Inputs
{
    public class Key : IPanelAction
    {
        [UserProperty]
        public VirtualKeyCode KeyCode { get; set; } = VirtualKeyCode.OEM_1;

        [UserProperty]
        public bool Down { get; set; } = true;

        public object? Run()
        {
            if (Down)
                InternalStatics.InputSimulator.Keyboard.KeyDown(KeyCode);
            else
                InternalStatics.InputSimulator.Keyboard.KeyUp(KeyCode);

            return null;
        }
    }
}
