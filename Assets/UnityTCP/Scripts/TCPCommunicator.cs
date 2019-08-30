using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Kodai100.Tcp
{

    public class TcpCommunicator : IDisposable
    {

        private TcpClient TcpClient { get; }

        protected Socket Socket => TcpClient?.Client;

        SynchronizationContext mainContext;
        OnMessageEvent OnMessage;

        bool running = false;

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
                    return false; // 強制で切断した場合に Socket が null になるため、例外を無視
                }
            }
        }

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
                // await後にUnityメインスレッドに戻っては困るような場合、ConfigureAwaitをつける（そのままスレッドプール上で処理を続けたい場合）
                await Task.Run(() => Receive()).ConfigureAwait(false);
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




        static string ToHexString(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var sb = new StringBuilder(data.Length * 2);

            foreach (var item in data)
            {
                sb.Append($"{item:X2}");
            }

            return sb.ToString();
        }
    }
}