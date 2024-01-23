using PanelController.Controller;
using PanelController.PanelObjects;
using System.Windows;
using System.Windows.Controls;
using Windows.Media.ClosedCaptioning;

namespace ConsoleExtensions
{
    public class LogWindow : Window, IPanelObject
    {
        public TextBox LogBox = new()
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(5)
        };

        public LogWindow()
        {
            Show();
            return;
            foreach (var item in Logger.Logs)
                LogBox.Text += item.ToString("[/L][/F] /M");
            AddChild(LogBox);
            Main.Deinitialized += (sender, e) => { Close(); };
            Logger.Logged += Logger_Logged;
        }

        private void Logger_Logged(object? sender, Logger.HistoricalLog e)
        {
            LogBox.Dispatcher.Invoke(() =>
            {
                LogBox.Text += e.ToString("[/L][/F] /M");
            });
        }
    }
}
