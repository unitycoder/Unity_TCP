using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Kodai100.Tcp
{
    
    class TCPServer : IDisposable
    {
        
        IPEndPoint endpoint;

        TcpListener listener;
        List<TcpClient> clients = new List<TcpClient>();

        SynchronizationContext mainContext;
        volatile bool acceptLoop = true;
        
        OnMessageEvent OnMessage;

        OnEstablishedEvent OnEstablished;
        OnDisconnectedEvent OnDisconnected;

        public IReadOnlyList<TcpClient> Clients => clients;

        public TCPServer(IPEndPoint endpoint, OnMessageEvent onMessage)
        {
            this.endpoint = endpoint ?? throw new ArgumentNullException("endpoint should not be null");

            // Set Unity main thread
            mainContext = SynchronizationContext.Current;

            OnMessage = onMessage;
            
        }

        public TCPServer(IPEndPoint endpoint, OnMessageEvent onMessage, OnEstablishedEvent onEstablished, OnDisconnectedEvent onDisconnected) : this(endpoint, onMessage)
        {
            OnEstablished = onEstablished;
            OnDisconnected = onDisconnected;

        }
        
        public async Task Listen()
        {
            lock (this)
            {
                if (listener != null)
                    throw new InvalidOperationException("Already started");

                acceptLoop = true;
                listener = new TcpListener(endpoint);
            }

            listener.Start();

            while (acceptLoop)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    
                    var _ = Task.Run(() => OnConnectClient(client));

                }
                catch (ObjectDisposedException e)
                {
                    // thrown if the listener socket is closed
                }
                catch (SocketException e)
                {
                    // Some socket error
                }
            }
        }



        public void Stop()
        {

            lock (this)
            {

                if (listener == null)
                    throw new InvalidOperationException("Not started");

                acceptLoop = false;

                listener.Stop();
                listener = null;

            }


            lock (clients)
            {
                foreach (var c in clients)
                {
                    c.Close();
                }
            }

        }

        async Task OnConnectClient(TcpClient client)
        {
            var clientEndpoint = client.Client.RemoteEndPoint;
            
            mainContext.Post(_ => OnEstablished.Invoke(client), null);
            clients.Add(client);

            await NetworkStreamHandler(client);
            
            mainContext.Post(_ => OnDisconnected.Invoke(clientEndpoint), null);
            clients.Remove(client);
        }
        
        
        async Task NetworkStreamHandler(TcpClient client)
        {

            while (client.Connected)
            {
                using (var stream = client.GetStream())
                {
                    var reader = new StreamReader(stream, Encoding.UTF8);

                    while (!reader.EndOfStream)
                    {
                        // TODO : currently, supported string decode only
                        var str = await reader.ReadLineAsync();
                        mainContext.Post(_ => OnMessage.Invoke(str), null);
                    }

                }

            }

            // Disconnected
        }


        public void BroadcastToClients(byte[] data)
        {
            foreach (var c in Clients)
            {
                c.GetStream().Write(data, 0, data.Length);
                c.GetStream().Flush();
            }
        }


        public void SendMessageToClient(TcpClient c, byte[] data)
        {
            c.GetStream().Write(data, 0, data.Length);
            c.GetStream().Flush();
        }


        public void Dispose()
        {
            Stop();
        }
    }

}