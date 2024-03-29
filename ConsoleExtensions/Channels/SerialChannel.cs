﻿using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using System.IO.Ports;
using System.Text;

namespace ConsoleExtensions.Channels
{
    [ItemName("Serial Channel")]
    public class SerialChannel : IChannel
    {
        [ItemName]
        public string ChannelName
        {
            get => $"Serial port {Port.PortName}";
        }

        public SerialPort Port = new();

        public bool IsOpen => Port.IsOpen;

        public event EventHandler<byte[]>? BytesReceived;

        [UserProperty]
        public int WaitOpenMilliseconds { get; set; } = 50;

        public SerialChannel()
        {
            Port.DataReceived += (sender, e) =>
            {
                BytesReceived?.Invoke(this, Encoding.UTF8.GetBytes(Port.ReadExisting()));
            };
            Port.PortName = "COM4";
            Port.BaudRate = 115200;
            Port.DtrEnable = true;
            Port.RtsEnable = true;
        }

        public SerialChannel(string portName, int buadrate, int waitOpenMilliseconds = 100)
            : this()
        {
            Port.PortName = portName;
            Port.BaudRate = buadrate;
            WaitOpenMilliseconds = waitOpenMilliseconds;
        }

        public object? Open()
        {
            try
            {
                Port.Open();
            }
            catch (Exception e)
            {
                return e;
            }

            //Delay for devices that reset after serial port opens (arduino replicas).
            Task.Delay(WaitOpenMilliseconds < 0 ? 0 : WaitOpenMilliseconds).Wait();
            return null;
        }

        public object? Send(byte[] data)
        {
            try
            {
                Port.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        public void Close()
        {
            if (Port.IsOpen)
                Port.Close();
        }

        private static readonly List<string> s_oldPortNames = new();

        [IChannel.Detector]
        public static IChannel[] Detect()
        {
            List<IChannel> channels = new();

            string[] currentPortNames = SerialPort.GetPortNames();

            try
            {
                foreach (var old in s_oldPortNames)
                {
                    if (currentPortNames.Contains(old))
                        continue;
                    s_oldPortNames.Remove(old);
                }
            }
            catch (InvalidOperationException)
            {
                return Detect();
            }

            foreach (var current in currentPortNames)
            {
                if (s_oldPortNames.Contains(current))
                    continue;
                channels.Add(new SerialChannel(current, 115200));
                s_oldPortNames.Add(current);
            }

            return channels.ToArray();
        }
    }
}
