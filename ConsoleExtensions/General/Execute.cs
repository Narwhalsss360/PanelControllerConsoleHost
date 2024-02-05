using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using System.Diagnostics;
using System.IO;

namespace ConsoleExtensions.General
{
    public class Execute : IPanelAction
    {
        [UserProperty]
        public string ExecutablePath { get; set; } = string.Empty;

        private Process? _process;

        public bool IsRunning
        {
            get
            {
                if (_process is null)
                    return false;
                return !_process.HasExited;
            }
        }

        public Execute()
        {
        }

        public Execute(string executablePath)
        {
            ExecutablePath = executablePath;
        }

        public object? Run()
        {
            FileInfo fileInfo = new(ExecutablePath);
            if (_process is not null)
                return "Process already running.";
            if (!fileInfo.Exists)
                return "File does not exist";
            if (fileInfo.Extension.ToLower() != ".exe")
                return "File type not supported";
            _process = Process.Start(fileInfo.FullName);
            _process.Exited += (sender, args) => { _process = null; };
            return null;
        }
    }
}
