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
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
        };

        public LogWindow()
        {
            Main.Deinitialized += (sender, e) => { Close(); };
            Logger.Logged += Logger_Logged;
            Closed += LogWindow_Closed;

            foreach (var item in Logger.Logs)
                LogBox.Text += item.ToString("/T [/L][/F] /M\n");
            AddChild(LogBox);

            LogBox.TextChanged += LogBox_TextChanged;

            Show();
        }

        private void LogBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogBox.CaretIndex = LogBox.Text.Length;
            LogBox.ScrollToEnd();
        }

        private void Logger_Logged(object? sender, Logger.HistoricalLog e) => LogBox.Dispatcher.Invoke(() => { LogBox.Text += e.ToString("/T [/L][/F] /M\n"); });

        private void LogWindow_Closed(object? sender, EventArgs e)
        {
            for (int i = 0; i < Extensions.Objects.Count; i++)
            {
                if (!ReferenceEquals(this, Extensions.Objects[i]))
                    continue;

                Extensions.Objects.RemoveAt(i);
                break;
            }
        }
    }
}
