using PanelController.PanelObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NStreamCom;
using System.Windows.Controls;
using System.Windows.Input;
using PanelController.Profiling;
using PanelController.Controller;
using Windows.System.Profile;
using System.Collections.Specialized;

namespace ConsoleExtensions
{
    public class VirtualPanelButton : Button
    {
        public readonly uint ID;

        public event EventHandler<Message>? ButtonUpdate;

        public VirtualPanelButton(uint ID)
        {
            this.ID = ID;
            Height = 40;
            Margin = new Thickness(5);
            Content = $"Button {ID}";
            PreviewMouseDown += ButtonMouseEvent;
            PreviewMouseUp += ButtonMouseEvent;
        }

        private void ButtonMouseEvent(object? sender, MouseEventArgs args)
        {
            List<byte> data = new();
            data.AddRange(BitConverter.GetBytes(ID));
            data.Add((byte)(args.LeftButton == MouseButtonState.Pressed ? 1 : 0));
            ButtonUpdate?.Invoke(this, new Message((ushort)ConnectedPanel.ReceiveIDs.DigitalStateUpdate, data.ToArray()));
        }
    }

    public class VirtualPanelAnalog : Slider
    {
        public readonly uint ID;

        public event EventHandler<Message>? AnalogUpdate;

        public VirtualPanelAnalog(uint ID)
        {
            this.ID = ID;
            Margin = new Thickness(5);
            ValueChanged += (sender, args) =>
            {
                List<byte> data = new();
                data.AddRange(BitConverter.GetBytes(ID));
                data.AddRange(BitConverter.GetBytes((uint)(uint.MaxValue * args.NewValue)));
                AnalogUpdate?.Invoke(this, new Message((ushort)ConnectedPanel.ReceiveIDs.AnalogStateUpdate, data.ToArray()));
            };
        }
    }

    public class VirtualPanelDisplay : TextBlock
    {
        public readonly uint ID;

        public VirtualPanelDisplay(uint ID)
        {
            this.ID = ID;
            Text = $"{ID}:";
        }

        public void SetDisplayValue(string value)
        {
            Text = $"{ID}:";
        }
    }

    public partial class VirtualPanel : Window, IChannel
    {
        public readonly Guid VirtualGuid = Guid.NewGuid();

        public readonly uint DigitalCount;

        public readonly uint AnalogCount;

        public readonly uint DisplayCount;

        public byte[] Handshake
        {
            get
            {
                List<byte> data = new();
                data.AddRange(VirtualGuid.ToByteArray());
                data.AddRange(BitConverter.GetBytes(DigitalCount));
                data.AddRange(BitConverter.GetBytes(AnalogCount));
                data.AddRange(BitConverter.GetBytes(DisplayCount));
                return data.ToArray();
            }
        }

        public bool IsOpen { get => true; }

        public event EventHandler<byte[]>? BytesReceived;

        private PacketCollector _collector = new();

        PanelInfo? ThisPanelInfo
        {
            get
            {
                if (Main.PanelsInfo.Find(info => info.PanelGuid == VirtualGuid) is not PanelInfo info)
                    return null;
                return info;
            }
        }

        public VirtualPanel(uint digitalCount, uint analogCount, uint displayCount)
        {
            _collector.PacketsReady += PacketsReady;
            DigitalCount = digitalCount;
            AnalogCount = analogCount;
            DisplayCount = displayCount;
            InitializeComponent();

            Logger.Logged += (sender, log) =>
            {
                LastLogBox.Dispatcher.Invoke(() =>
                {
                    LastLogBox.Text = log.ToString("/T [/L][/F] /M");
                });
            };
            Main.PanelsInfo.CollectionChanged += RefreshName;
            PanelNameBox.KeyDown += RefreshName;
            InitializeVirtualInterfaces();

            Show();
        }

        private void InterfaceUpdateMessage(object? sender, Message message) => BytesReceived?.Invoke(this, message.GetPackets((ushort)message.Data.Length)[0].GetStreamBytes());

        private void RefreshName(object? sender, EventArgs args)
        {
            if (args is NotifyCollectionChangedEventArgs changedArgs)
            {
                PanelNameBox.Text = ThisPanelInfo?.Name;
            }
            else if (args is KeyEventArgs keyArgs)
            {
                if (keyArgs.Key == Key.Enter)
                    if (ThisPanelInfo is PanelInfo info)
                        info.Name = PanelNameBox.Text;
            }
        }

        private void InitializeVirtualInterfaces()
        {
            for (uint i = 0; i < DigitalCount; i++)
            {
                VirtualPanelButton virtualButton = new(i);
                virtualButton.ButtonUpdate += InterfaceUpdateMessage;
                ButtonStack.Children.Add(virtualButton);
            }

            for (uint i = 0; i < AnalogCount; i++)
            {
                VirtualPanelAnalog virtualAnalog = new(i);
                virtualAnalog.AnalogUpdate += InterfaceUpdateMessage;
                AnalogStack.Children.Add(virtualAnalog);
            }

            for (uint i = 0; i < DisplayCount; i++)
            {
                VirtualPanelDisplay virtualDisplay = new(i);
                DisplayStack.Children.Add(virtualDisplay);
            }
        }

        public object? Open()
        {
            return null;
        }

        private void PacketsReady(object? sender, PacketsReadyEventArgs args)
        {
            Message message = new(args.Packets);

            if (message.ID == 0)
            {
                BytesReceived?.Invoke(this, Handshake);
            }
            else if (message.ID == 1)
            {
            }
        }

        public object? Send(byte[] bytes)
        {
            try
            {
                _collector.Collect(bytes);
            }
            catch (PacketsLost)
            {
            }
            catch (SizeMismatch)
            {
            }

            return null;
        }
    }
}
