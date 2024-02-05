using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using System.IO.Pipes;

namespace ConsoleExtensions.Channels
{
    [ItemName("Named Pipe")]
    public class NamedPipe : IChannel
    {
        public bool IsOpen => throw new NotImplementedException();

        public event EventHandler<byte[]>? BytesReceived;

        public NamedPipe()
        {
            throw new NotImplementedException("Incomplete");
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public object? Open()
        {
            throw new NotImplementedException();
        }

        public object? Send(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
