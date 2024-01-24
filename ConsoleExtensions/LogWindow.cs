using PanelController.Controller;
using PanelController.PanelObjects;
using System.Windows;
using System.Windows.Controls;

namespace ConsoleExtensions
{
    public class LogWindow : Window, IPanelObject
    {
        public TextBox LogBox = new()
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(10),
            IsEnabled = false,
            TextWrapping = TextWrapping.Wrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
        };

        public LogWindow()
        {
            foreach (var item in Logger.Logs)
                LogBox.Text += item.ToString("[/L][/F] /M\n");
            AddChild(LogBox);
            Main.Deinitialized += (sender, e) => { Close(); };
            Logger.Logged += Logger_Logged;
            Show();
        }

        private void Logger_Logged(object? sender, Logger.HistoricalLog e) => LogBox.Dispatcher.Invoke(() => { LogBox.Text += e.ToString("[/L][/F] /M\n"); });
    }
}
