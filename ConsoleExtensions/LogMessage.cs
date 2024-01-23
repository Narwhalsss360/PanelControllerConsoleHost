using PanelController.Controller;
using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;

namespace ConsoleExtensions
{
    [ItemName("Log Message")]
    [ItemDescription("/T: DateTime")]
    public class LogMessage : IPanelAction
    {
        private delegate string FormmatingDelegate();

        [UserProperty]
        private Dictionary<string, FormmatingDelegate> Formattings { get; } = new()
        {
            { "/T", () => { return DateTime.Now.ToString(); } }
        };

        public string Message { get; set; } = string.Empty;

        public string Sender { get; set; } = "";

        public Logger.Levels Level { get; set; } = Logger.Levels.Info;

        public object? Run()
        {
            string log = Message;
            foreach (var formattings in Formattings)
                log.Replace(formattings.Key, formattings.Value());
            Logger.Log(log, Level, string.IsNullOrEmpty(Sender) ? this : Sender);
            return null;
        }
    }
}
