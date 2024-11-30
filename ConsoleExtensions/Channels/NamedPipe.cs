using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using System.IO.Pipes;

namespace ConsoleExtensions.Channels
{
    public static class PipeStreamExtension
    {
        public static byte[] ReadNextMessage(this PipeStream pipe, int timeoutMilliseconds = 1)
        {
            if (pipe.ReadMode != PipeTransmissionMode.Message)
            {
                throw new Exception($"{ReadNextMessage} requires pipe read mode to be Message");
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            byte[] firstByteBuffer = new byte[1];
            Task readTask = pipe.ReadAsync(firstByteBuffer, 0, 1, cts.Token);
            bool timeout = Task.WaitAny(readTask, Task.Delay(timeoutMilliseconds)) == 1;

            if (readTask.IsFaulted && readTask.Exception is not null)
            {
                throw readTask.Exception;
            }

            if (timeout)
            {
                return new byte[0];
            }

            List<byte> message = new List<byte>() { firstByteBuffer[0] };
            while (!pipe.IsMessageComplete)
            {
                message.Add((byte)pipe.ReadByte());
            }

            return message.ToArray();
        }
    }

    [ItemName("Named Pipe")]
    public class NamedPipe : IChannel
    {
        public bool IsOpen => throw new NotImplementedException();

        public event EventHandler<byte[]>? BytesReceived;

        private NamedPipeServerStream _pipe;

        private Thread? _readerThread = null;

        public NamedPipe(string pipeName)
        {
            _pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, 512, 512);
            
        }

        private void ReaderThread()
        {
            while (_pipe.IsConnected)
            {
                byte[] data = _pipe.ReadNextMessage();
                if (data.Length == 0)
                {
                    Thread.Sleep(1);
                }
                BytesReceived?.Invoke(this, data);
            }
        }

        public void Close()
        {
            if (_pipe.IsConnected)
            {
                _pipe.Close();
            }

            if (_readerThread is not null)
            {
                _readerThread.Join();
                _readerThread = null;
            }
            
        }

        public object? Open()
        {
            if (_pipe.IsConnected)
            {
                return null;
            }

            if (_readerThread is not null)
            {
                return new Exception($"Program error: {nameof(_readerThread)} is not null in ${nameof(Open)}");
            }

            _pipe.WaitForConnection();
            _readerThread = new Thread(ReaderThread);
            _readerThread.Start();
            return null;
        }

        public object? Send(byte[] data)
        {
            _pipe.Write(data, 0, data.Length);
            return null;
        }
    }
}
