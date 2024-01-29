using PanelController.Controller;
using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace ConsoleExtensions
{
    public class LogWindow : Window, IPanelObject
    {
        public TextBox LogBox = new()
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(10),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
        };

        private string _logFormat = "/T [/L][/F] /M";

        [UserProperty]
        public string LogFormat
        {
            get
            {
                return _logFormat;
            }
            set
            {
                _logFormat = value;
                if (!_logFormat.EndsWith("\n"))
                    _logFormat = "\n";
                Dispatcher.Invoke(RefreshLogs);
            }
        }


        public LogWindow()
        {
            Main.Deinitialized += (sender, e) => { Close(); };
            Logger.Logged += Logger_Logged;
            Closed += LogWindow_Closed;
            AddChild(LogBox);
            RefreshLogs();
            LogBox.TextChanged += LogBox_TextChanged;
            Extensions.Objects.CollectionChanged += Objects_CollectionChanged;
            Show();
        }

        private void Objects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                if (args.OldItems is null)
                    return;
                if (args.OldItems.Contains(this))
                    Close();
            }
        }

        private void RefreshLogs()
        {
            LogBox.Text = "";
            foreach (var log in Logger.Logs)
                LogBox.Text += log.ToString(_logFormat);
        }

        private void LogBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogBox.CaretIndex = LogBox.Text.Length;
            LogBox.ScrollToEnd();
        }

        private void Logger_Logged(object? sender, Logger.HistoricalLog e) => LogBox.Dispatcher.Invoke(() => { LogBox.Text += e.ToString(_logFormat); });

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
