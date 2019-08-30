using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kodai100.Tcp
{

    public class TcpCommunicator : IDisposable
    {
        
        public string Name { get; }

        public bool IsConnecting {
            get {
                try
                {
                    if ((TcpClient == null) || !TcpClient.Connected) return false;
                    if (Socket == null) return false;

                    return !(Socket.Poll(1, SelectMode.SelectRead) && (Socket.Available <= 0));
                }
                catch
                {
                    return false;
                }
            }
        }


        private TcpClient TcpClient { get; }

        private Socket Socket => TcpClient?.Client;

        private SynchronizationContext mainContext;
        private OnMessageEvent OnMessage;

        private bool running = false;


        public TcpCommunicator(TcpClient tcpClient, OnMessageEvent onMessage)
        {
            this.TcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            this.Name = $"[{Socket.RemoteEndPoint}]";

            this.mainContext = SynchronizationContext.Current;
            this.OnMessage = onMessage;
        }

        public TcpCommunicator(string host, int port, OnMessageEvent onMessage) : this(new TcpClient(host, port), onMessage)
        {
        }

        public void Dispose()
        {
            if (TcpClient != null)
            {
                running = false;

                TcpClient.Close();
                (TcpClient as IDisposable).Dispose();
                
            }
        }



        public void Send(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!IsConnecting) throw new InvalidOperationException();

            try
            {
                var stream = TcpClient.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Attempt to send failed.", ex);
            }
        }


        public async Task Listen()
        {

            if (TcpClient == null) return;
            
            running = true;

            while (running)
            {
                await Task.Run(() => Receive());
            }
            
        }
        
        public async Task Receive()
        {
            if (!IsConnecting)
            {
                throw new InvalidOperationException();
            }

            try
            {
                var stream = TcpClient.GetStream();

                while (stream.DataAvailable)
                {
                    var reader = new StreamReader(stream, Encoding.UTF8);

                    var str = await reader.ReadLineAsync();

                    mainContext.Post(_ => OnMessage.Invoke(str), null);
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("Attempt to receive failed.", ex);
            }
        }
        
    }
}